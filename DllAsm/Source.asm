;-------------------------------------------------------------------------
.DATA
; Zmienne
Maski BYTE 9 DUP (?) ; Maski filtru
SumaMasek QWORD ? ; Suma masek
PrzesuniecieZnakow BYTE 16 DUP (10000000y)	; Do sumowania masek z uwzględnieniem znaku

.CODE
; Kod źródłowy procedur

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
; Procedura wywoływana automatycznie przez środowisko w momencie pierwszego wejścia do DLL.
; Używamy jej, by zainicjalizować i zsumować maski zanim algorytm zostanie wywołany.

CALL InicjalizujMaski
CALL SumujMaski

MOV	EAX, 1 ; zwracamy true
RET

DllEntry ENDP

SumujMaski PROC
; Procedura sumująca maski do zmiennej SumaMasek.

; Przechowujemy wartosci w stosie na czas wywolania funkcji by nie utracic wartosci
PUSH RAX 
PUSH RDX

XOR RDX, RDX ; zerujemy rejestr

;! Wykorzystane instrukcje wektorowe - MOVQ (MMX), PSADBW (SSE2)
MOVQ XMM0, QWORD PTR [Maski] ; zapisujemy do rejestru wektorowego wartości masek (qword - 8 bajtów)
MOVSX EAX, BYTE PTR [Maski+8] ; dziewiąty bajt osobno, przypisujemy do EAX (rej. wek. operuja na 8 bajtach dlatego dziewiaty recznie)

MOVDQU XMM2, XMMWORD PTR [PrzesuniecieZnakow] ; do XMM2 przypisujemy to przesuniecie (tablica wartosci o ile trzeba przesunac)

PXOR XMM0, XMM2 ; PXOR by usunac znak, otrzymujemy w XMM0 wartosci maski bez znaku

PXOR XMM1, XMM1 ; zerujemy XMM1
PSADBW XMM1, XMM0 ; sumowanie wartosci (wlasciwie to roznic pomiedzy XMM0 - wyzerowanym XMM1)

MOVD EDX, XMM1 ; zapisujemy sume do EDX

SUB RAX, 8 * 10000000y	; Ponownie odejmujemy 8 przesunięć znakowych
ADD RAX, RDX ; dodajemy do przesniecia wyliczoną sume w RDX
MOV SumaMasek, RAX ; wpisujemy sume do zmiennej globalnej 'sumaMasek'

; Przywrocenie wartosci ze stosu
POP RDX
POP RAX
RET

SumujMaski ENDP

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
; Procedura przyjmuje parametr (wskaźnik na tablicę wejściową zawierającą wartości R, G lub B z obszaru 3x3) w rejestrze RCX.
; Procedura zwraca wartość oznaczającą nową wartość piksela środkowego w danym kolorze, w rejestrze RAX.
; Przykład wywołania:
; wejście: () oznacza piksel, dla którego liczymy nową wartość
; 174 200 100
; 179 (20) 103
; 0	 201  300
; wyjście: (dla filtra LAPL1) = -200 - 179 - 103 - 201 + 4 * 20 < 0 -> *0*

PUSH R8	; zapisujemy stos

MOV R8, RCX	; adres tablicy wejściowej w R8 3x3

MOVQ XMM1, QWORD PTR [Maski] ; przenosimy 8 elementow masek do wektora XMM1
MOVQ XMM2, QWORD PTR [R8] ; przenosimy 8 elementow tablicy wartosci pikseli do wektora XMM1

; Wykorzystane instrukcje wektorowe: PMOVSXBW (SSE4), PMOVZXBW (SSE4), PMADDWD (MMX), PHADDD (SSE3)
; Konwertuja wszystkie wartosci w wektorze z 8 na 16 bitową w celu przemnożenia ich
PMOVSXBW XMM1, XMM1		; PMOVSXBW - 1 sposob konwersji
PMOVZXBW XMM2, XMM2    ; PMOVSXBW - 2 sposob konwersji
PMADDWD	XMM1, XMM2	; PMADDWD mnoży odpowiednie elementy dwóch wektorów i sumuje je parami [8x8 parami, potem zostaje 8 i sumuje sie 1z2, 3z4 itd i zostają 4]
PHADDD XMM1, XMM1 ; sumuje parami tą czwórkę otrzymaną wyżej - teraz sumuj 1z2 i 3z4 i zostają 2
PHADDD XMM1, XMM1 ; sumuje pozostałe 2 i zostaje jedna wartość
MOVD EBX, XMM1 ; zapisuje wynik (zapisany w XMM1) do EBX
MOVSXD RAX, EBX ; przenosi do RAX

MOVSX RCX, BYTE PTR [Maski+8] ; przenosimy 9 element masek do RCX
MOVZX RBX, BYTE PTR [R8+8]; przenosimy 9 element tablicy wartosci pikseli do RBX
IMUL RCX, RBX	; mnożenie 
ADD RAX, RCX ; dodawanie

OBLICZKONIEC:
CALL Clamp ; clampowanie <0;255>
MOV RBX, SumaMasek	; suma masek w RBX
CMP RBX, 0 
JNE OBLICZPODZIELPRZEZSUME ; != 0 -> dzielimy przez sume
JMP OBLICZKONIECKONIEC ; skok do konca
OBLICZPODZIELPRZEZSUME:
; Dzielimy przez sumę masek, jeśli różna od zera
PXOR XMM0, XMM0	; zerowanie wektora XMM0 - wartosc piksela
;! Wykorzystane instrukcje wektorowe: PXOR (MMX), DIVSS (SSE), CVTSI2SS (SSE), CVTTSS2SI (SSE)
CVTSI2SS XMM0, RAX	; zamieniamy int w RAX na float i zapisujemy do XMM0
PXOR XMM1, XMM1 ; zerowanie wektora XMM1 - suma masek
CVTSI2SS XMM1, RBX; zamieniamy int w RBX na float i zapisujemy do XMM1
DIVSS XMM0, XMM1 ; dzielimy zmiennoprzecinkowo (wart. piks/suma masek)
CVTTSS2SI RAX, XMM0 ; float w XMM0 konwertujemy na int i wrzucamy do RAX
OBLICZKONIECKONIEC:

POP R8 ; przywracamy stos

; wartosc zwracana jest w RAX
RET

ObliczNowaWartoscPiksela ENDP

NalozFiltrAsm PROC
; Procedura nakładająca filtr Laplace'a (LAPL1) na fragment bitmapy.
; Parametry procedury:
; wskaznikNaWejsciowaTablice - zapisany do RCX
; wskaznikNaWyjsciowaTablice - zapisany do RDX
; tablicaPomocniczaR - zapisany do R8
; tablicaPomocniczaG - zapisany do R9
; tablicaPomocniczaB - piąty parametr na stosie
; dlugoscBitmapy - szósty parametr na stosie
; szerokoscBitmapy - siódmy parametr na stosie
; indeksStartowy - ósmy parametr na stosie
; ileIndeksowFiltrowac - dziewiąty parametr na stosie
; Procedura nie zwraca wyniku (wynik odczytywany jest za pomocą jednego ze wskaźników wyjściowych).

; Przenosimy parametry do rejestrów
MOV R11, RCX	; R11 - wskaznikNaWejsciowaTablice
MOV R12, RDX	; R12 - wskaznikNaWyjsciowaTablice
MOV R13, R8	; R13 - tablicaPomocniczaR
MOV R14, R9	; R14 - tablicaPomocniczaG
MOV R15, QWORD PTR [RSP+40]	; R15 - tablicaPomocniczaB

XOR R8, R8 ; zerujemy R8
XOR R9, R9 ; zerujemy R9

; Główna pętla - iterowanie się po tablicy bajtów (wejściowej)
JMP STARTGLOWNEJPETLI
STARTGLOWNEJPETLI:
	; R8 = i 
	MOV R8, QWORD PTR [RSP+64]	; (wartosc poczatkowa to indeks startowy)

GLOWNAPETLA:		
	MOV R9, QWORD PTR [RSP+56] ; wrzucamy szerokość bitmapy
	CMP R8, R9	; pierwszy rząd bitmapy (pomijamy)
	JL KONIECGLOWNEJPETLI ; continue

	MOV RAX, R8	; lewa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
	XOR RDX, RDX
	MOV RCX, QWORD PTR [RSP+56]
	DIV RCX
	CMP RDX, 0
	JE KONIECGLOWNEJPETLI ; continue

	MOV RCX, QWORD PTR [RSP+48] ; ostatni rząd bitmapy (pomijamy) -> odejmujemy dlugosc od szerokosci
	SUB RCX, QWORD PTR [RSP+56]
	CMP R8, RCX
	JGE KONIECGLOWNEJPETLI ; continue

	MOV RAX, R8		; prawa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
	ADD RAX, 2
	INC RAX
	XOR RDX, RDX
	MOV RCX, QWORD PTR [RSP+56] ; przenosimy szerokosc bitmapy
	DIV RCX ; RAX / RCX (dziel calkowite)
	CMP RDX, 0	
	JE KONIECGLOWNEJPETLI ; continue

	XOR R9, R9
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
			IMUL RAX, QWORD PTR [RSP+56]	; szerokosc bitmapy
			ADD RCX, RAX
			ADD RCX, R8

			; RDX = x * 3 + y;
			MOV RDX, R9
			IMUL RDX, 3
			ADD RDX, R10

			; bierzemy z wejścia odpowiednią wartość piksela i zapisujemy ja do odpowiedniej tablicy (r/g/b)

			MOV AL, BYTE PTR [R11 + RCX]	; R11 - wskaźnik na wejściową tablicę, RCX zawiera obliczony indeks piksela
			MOV BYTE PTR [R13 + RDX], AL	; R13 - wskaźnik na tablicę R, RDX zawiera obliczony indeks w tej tablicy
			INC RCX	; indeksPikela++

			MOV AL, BYTE PTR [R11 + RCX]
			MOV BYTE PTR [R14 + RDX], AL	; R14 - wskaźnik na tablicę G
			INC RCX	; indeksPikela++

			MOV AL, BYTE PTR [R11 + RCX]
			MOV BYTE PTR [R15 + RDX], AL	; R15 - wskaźnik na tablicę B

			INC R10 ; x++
			CMP R10, 3 ; jesli x nie jest trojka to skaczemy do wewnetrzej spowrotem
			JNE PETLAWEWNETRZNA

			INC R9 ; y++
			JMP PETLAZEWNETRZNA ; skaczemy do zewnetrznej spowrotem

KONIECPODWOJNEJPETLI:	; wartości zwracane z procedury ObliczNowaWartoscPiksela znajdują się w dolnym bajcie rejestru RAX (->AL)

	MOV RCX, R13 ; przekazujemy R do RCX by wywolac funkcje obliczajaca
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = i - indeksStartowy;
	MOV RDX, R8 
	SUB RDX, QWORD PTR [RSP+64]
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze R
	MOV BYTE PTR [R12 + RDX], AL

	MOV RCX, R14 ; przekazujemy G do RCX by wywolac funkcje obliczajaca
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = (i - indeksStartowy)++ ;
	MOV RDX, R8
	SUB RDX, QWORD PTR [RSP+64]
	INC RDX
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze G
	MOV BYTE PTR [R12 + RDX], AL

	MOV RCX, R15 ; przekazujemy B do RCX by wywolac funkcje obliczajaca
	CALL ObliczNowaWartoscPiksela
	; RDX =  indeksPikselaWyjscie = (i - indeksStartowy) + 2 ;
	MOV RDX, R8
	SUB RDX, QWORD PTR [RSP+64]
	INC RDX
	INC RDX
	; przepisujemy do tablicy R12 wyjsciowej wartosc piksela (tego co wyliczylismy) w kolorze B
	MOV BYTE PTR [R12 + RDX], AL

	JMP KONIECGLOWNEJPETLI

KONIECGLOWNEJPETLI:
	ADD R8, 3 ; i+=3

	; RAX = indeks startowy + ileElementówFiltrować
	MOV RAX, QWORD PTR [RSP+64] 
	ADD RAX, QWORD PTR [RSP+72]

	CMP R8, RAX
	JL GLOWNAPETLA ; jeżeli i < RAX, to iterujemy dalej 

JMP KONIEC
KONIEC:
XOR RAX, RAX	; upewniamy się, że nie ma wartości zwracanej (bo nasza funkcja to void)
RET

NalozFiltrAsm ENDP

END
;-------------------------------------------------------------------------
