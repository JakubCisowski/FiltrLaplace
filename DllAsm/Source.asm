;-------------------------------------------------------------------------
.DATA
; Zmienne
Maski BYTE 9 DUP (?) ; Maski filtru
PrzesuniecieZnakow BYTE 16 DUP (10000000y)	; Do sumowania masek z uwzględnieniem znaku

.CODE
; Kod źródłowy procedur

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
; Procedura wywoływana automatycznie przez środowisko w momencie pierwszego wejścia do DLL.
; Używamy jej, by zainicjalizować i zsumować maski zanim algorytm zostanie wywołany.

CALL InicjalizujMaski

MOV	EAX, 1 ; zwracamy true
RET

DllEntry ENDP

InicjalizujMaski PROC
; Procedura inicjalizująca maski.
; http://www.algorytm.org/przetwarzanie-obrazow/filtrowanie-obrazow.html - filtr LAPL1

PUSH RCX

LEA RCX, Maski ; ładujemy adres zmiennej globalnej maski do RCX

; ładujemy do odpowiedniego adresu wartość maski
MOV BYTE PTR [RCX], 0
MOV BYTE PTR [RCX+2], 0
MOV BYTE PTR [RCX+6], 0
MOV BYTE PTR [RCX+8], 0
MOV BYTE PTR [RCX+1], -1
MOV BYTE PTR [RCX+3], -1
MOV BYTE PTR [RCX+5], -1
MOV BYTE PTR [RCX+7], -1
MOV BYTE PTR [RCX+4], 4

POP RCX

RET

InicjalizujMaski ENDP

Clamp PROC
; Odpowiednik funkcji std::clamp z C++, funkcja ta sprawia, że wartość wejściowa (przekazana w rejestrze RAX) znajduje się w przedziale <0; 255>.
; Jeżeli RAX std::< 0, wówczas RAX std::= 0
; Jeżeli RAX std::> 255, wówczas RAX std::= 255
; W innym wypadku RAX zostaje bez zmian.

CMP RAX, 0
JL CLAMPZERO ; JL - jump less, skacze jesli mniejsza niz zero
CMP RAX, 255
JG CLAMP255 ; JG - jump if greater, skacze jesli wieksze niz 255
RET
CLAMPZERO:
MOV RAX, 0 ; ustawiamy na 0
RET
CLAMP255:
MOV RAX, 255 ; ustawiamy na 255
RET

Clamp ENDP

ObliczNowaWartoscPiksela PROC
; Procedura obliczająca nową wartość piksela (tylko w jednym kolorze w ciągu jednego wywołania - R, G lub B) na podstawie pikseli ułożonych w siatkę 3x3.
; Na podstawie tablicy wejściowej i tablicy masek obliczane i sumowane są poszczególne wagi pikseli, a następnie nowa wartość dzielona jest przez sumę masek (jeśli różna od 0).
; Procedura przyjmuje parametr (wartości R, G lub B pikseli z obszaru 3x3) w rejestrze XMM7.
; Procedura zwraca wartość oznaczającą nową wartość piksela środkowego w danym kolorze, w rejestrze RAX.
; Przykład wywołania:
; wejście: () oznacza piksel, dla którego liczymy nową wartość
; 174 200 100
; 179 (20) 103
; 0	 201  300
; wyjście: (dla filtra LAPL1) = -200 - 179 - 103 - 201 + 4 * 20 < 0 -> *0*


MOVQ XMM1, QWORD PTR [Maski] ; przenosimy 8 elementow masek do wektora XMM1
MOVDQU XMM2, XMM7 ; zapisane elementy z XMM7 do XMM2

; Wykorzystane instrukcje wektorowe: PMOVSXBW (SSE4), PMOVZXBW (SSE4), PMADDWD (MMX), PHADDD (SSE3)
; Konwertuja wszystkie wartosci w wektorze z 8 na 16 bitową w celu przemnożenia ich
PMOVSXBW XMM1, XMM1		; PMOVSXBW - 1 sposob konwersji
;PMOVZXBW XMM2, XMM2    ; PMOVSXBW - 2 sposob konwersji - nie korzystamy, ponieważ w XMM2 zapisane są już 16-bitowe wartości
PMADDWD	XMM1, XMM2	; PMADDWD mnoży odpowiednie elementy dwóch wektorów i sumuje je parami [8x8 parami, potem zostaje 8 i sumuje sie 1z2, 3z4 itd i zostają 4]
PHADDD XMM1, XMM1 ; sumuje parami tą czwórkę otrzymaną wyżej - teraz sumuj 1z2 i 3z4 i zostają 2
PHADDD XMM1, XMM1 ; sumuje pozostałe 2 i zostaje jedna wartość
MOVD EBX, XMM1 ; zapisuje wynik (zapisany w XMM1) do EBX
MOVSXD RAX, EBX ; przenosi do RAX

OBLICZKONIEC:
CALL Clamp ; clampowanie <0;255>
; wartosc zwracana jest w RAX
RET

ObliczNowaWartoscPiksela ENDP

NalozFiltrAsm PROC
; Procedura nakładająca filtr Laplace'a (LAPL1) na fragment bitmapy.
; Parametry procedury:
; wskaznikNaWejsciowaTablice - zapisany do RCX
; wskaznikNaWyjsciowaTablice - zapisany do RDX
; dlugoscBitmapy - zapisany do R8
; szerokoscBitmapy - zapisany do R9
; indeksStartowy - piąty parametr na stosie
; ileIndeksowFiltrowac - szósty parametr na stosie
; Procedura nie zwraca wyniku (wynik odczytywany jest za pomocą jednego ze wskaźników wyjściowych).

; Przenosimy parametry do rejestrów
MOV R11, RCX	; R11 - wskaznikNaWejsciowaTablice
MOV R12, RDX	; R12 - wskaznikNaWyjsciowaTablice
MOV R13, R8	; R13 - dlugoscBitmapy
MOV R14, R9	; R14 - szerokoscBitmapy
MOV R15, QWORD PTR [RSP+40]	; R15 - indeksStartowy

XOR R8, R8 ; zerujemy R8
XOR R9, R9 ; zerujemy R9

; Główna pętla - iterowanie się po tablicy bajtów (wejściowej)
JMP STARTGLOWNEJPETLI
STARTGLOWNEJPETLI:
	; R8 = i 
	MOV R8, R15	; (wartosc poczatkowa to indeks startowy)

GLOWNAPETLA:		
	MOV R9, R14 ; wrzucamy szerokość bitmapy
	CMP R8, R9	; pierwszy rząd bitmapy (pomijamy)
	JL KONIECGLOWNEJPETLI ; continue

	MOV RAX, R8	; lewa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
	XOR RDX, RDX
	MOV RCX, R14
	DIV RCX
	CMP RDX, 0
	JE KONIECGLOWNEJPETLI ; continue

	MOV RCX, R13 ; ostatni rząd bitmapy (pomijamy) -> odejmujemy dlugosc od szerokosci
	SUB RCX, R14
	CMP R8, RCX
	JGE KONIECGLOWNEJPETLI ; continue

	MOV RAX, R8		; prawa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
	ADD RAX, 2
	INC RAX
	XOR RDX, RDX
	MOV RCX, R14 ; przenosimy szerokosc bitmapy
	DIV RCX ; RAX / RCX (dziel calkowite)
	CMP RDX, 0	
	JE KONIECGLOWNEJPETLI ; continue

	XOR R9, R9
	PXOR xmm13, xmm13
	PXOR xmm14, xmm14
	PXOR xmm15, xmm15
	; R9 = y
PETLAZEWNETRZNA: ; // Sczytujemy wartości z obszaru 3x3 wokół obecnego piksela i zapisujemy je do tablic r,g,b.
		; R10 = x
		XOR R10, R10
		CMP R9, 3 ; koniec petli jesli y dojdzie do 3
		JE KONIECPODWOJNEJPETLI

		JMP PETLAWEWNETRZNA
PETLAWEWNETRZNA:		
			; RCX = i + (szerokoscBitmapy * (y - 1) + (x - 1) * 3);
			MOV RCX, R10
			DEC RCX
			IMUL RCX, 3
			MOV RAX, R9
			DEC RAX
			IMUL RAX, R14	; szerokosc bitmapy
			ADD RCX, RAX
			ADD RCX, R8

			; RDX = x * 3 + y;
			MOV RDX, R9
			IMUL RDX, 3
			ADD RDX, R10

			; bierzemy z wejścia odpowiednią wartość piksela i zapisujemy ja do odpowiedniej tablicy (r/g/b)

			XOR RAX, RAX

			MOV AL, BYTE PTR [R11 + RCX]	; R11 - wskaźnik na wejściową tablicę, RCX zawiera obliczony indeks piksela

			PSLLDQ XMM13, 2
			MOVD XMM7, EAX
			ADDPS XMM13, XMM7

			INC RCX	; indeksPikela++

			MOV AL, BYTE PTR [R11 + RCX]

			PSLLDQ XMM14, 2
			MOVD XMM7, EAX
			ADDPS XMM14, XMM7

			INC RCX	; indeksPikela++

			MOV AL, BYTE PTR [R11 + RCX]

			PSLLDQ XMM15, 2
			MOVD XMM7, EAX
			ADDPS XMM15, XMM7

			INC R10 ; x++
			CMP R10, 3 ; jesli x nie jest trojka to skaczemy do wewnetrzej spowrotem
			JNE PETLAWEWNETRZNA

			INC R9 ; y++
			JMP PETLAZEWNETRZNA ; skaczemy do zewnetrznej spowrotem

KONIECPODWOJNEJPETLI:	; wartości zwracane z procedury ObliczNowaWartoscPiksela znajdują się w dolnym bajcie rejestru RAX (->AL)

	MOVDQU XMM7, XMM13
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = i - indeksStartowy;
	MOV RDX, R8 
	SUB RDX, R15
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze R
	MOV BYTE PTR [R12 + RDX], AL

	MOVDQU XMM7, XMM14
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = (i - indeksStartowy)++ ;
	MOV RDX, R8
	SUB RDX, R15
	INC RDX
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze G
	MOV BYTE PTR [R12 + RDX], AL

	MOVDQU XMM7, XMM15
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = (i - indeksStartowy) + 2 ;
	MOV RDX, R8
	SUB RDX, R15
	INC RDX
	INC RDX
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze B
	MOV BYTE PTR [R12 + RDX], AL

	JMP KONIECGLOWNEJPETLI

KONIECGLOWNEJPETLI:
	ADD R8, 3 ; i+=3

	; RAX = indeks startowy + ileElementówFiltrować
	MOV RAX, R15
	ADD RAX, QWORD PTR [RSP+48]	; to juz jest zamienione

	CMP R8, RAX
	JL GLOWNAPETLA ; jeżeli i < RAX, to iterujemy dalej 

JMP KONIEC
KONIEC:
XOR RAX, RAX	; upewniamy się, że nie ma wartości zwracanej (bo nasza funkcja to void)
RET

NalozFiltrAsm ENDP

END
;-------------------------------------------------------------------------
