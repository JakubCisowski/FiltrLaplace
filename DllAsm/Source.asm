;-------------------------------------------------------------------------
.DATA

; Zmienne

Maski BYTE 9 DUP (?)
SumaMasek QWORD ?
PrzesuniecieZnakow BYTE 16 DUP (10000000y)	; Znak znajduje się na pierwszym bicie od lewej

.CODE

; Kod źródłowy procedur

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
; Procedura wywoływana automatycznie przez środowisko w momencie pierwszego wejścia do DLL.
; Używamy jej, by zainicjalizować i zsumować maski zanim algorytm zostanie wywołany.

CALL InicjalizujMaski
CALL SumujMaski

MOV	EAX, 1
RET

DllEntry ENDP

SumujMaski PROC
; Procedura sumująca maski do zmiennej SumaMasek.

PUSH RAX
PUSH RDX
XOR RDX, RDX
;! Wykorzystane instrukcje wektorowe - MOVQ (MMX), PSADBW (SSE2)
MOVQ XMM0, QWORD PTR [Maski]
MOVSX EAX, BYTE PTR [Maski+8]
MOVDQU XMM2, XMMWORD PTR [PrzesuniecieZnakow]
PXOR XMM0, XMM2
PXOR XMM1, XMM1
PSADBW XMM1, XMM0
MOVD EDX, XMM1
SUB RAX, 8 * 10000000y	; Ponownie odejmujemy 8 przesunięć znakowych
ADD RAX, RDX
MOV SumaMasek, RAX
POP RDX
POP RAX
RET

SumujMaski ENDP

InicjalizujMaski PROC
; Procedura inicjalizująca maski.
; http://www.algorytm.org/przetwarzanie-obrazow/filtrowanie-obrazow.html - filtr LAPL1

PUSH RCX
LEA RCX, Maski
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
JL CLAMPZERO
CMP RAX, 255
JG CLAMP255
RET
CLAMPZERO:
MOV RAX, 0
RET
CLAMP255:
MOV RAX, 255
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
PUSH R9
PUSH R10
PUSH R11
MOV R8, RCX	; adres tablicy wejściowej w R8
MOV RAX, 0
MOV RBX, 0
LEA R9, Maski	; adres masek w R9
OBLICZPETLAZEWN:				
MOV RCX, 0
CMP RBX, 3
JE OBLICZKONIEC
JMP OBLICZPETLAWEWN
OBLICZPETLAWEWN:				
MOV R10, RBX
IMUL R10, 3
ADD R10, RCX	; R10 = 3 * y + x
MOVZX RDX, BYTE PTR [R8 + R10]
MOVSX R11, BYTE PTR [R9 + R10]
IMUL RDX, R11	
ADD RAX, RDX	; sumujemy wartość piksela
INC RCX
CMP RCX, 3
JNE OBLICZPETLAWEWN
INC RBX
JMP OBLICZPETLAZEWN
OBLICZKONIEC:
CALL Clamp
MOV RBX, SumaMasek	; suma masek w RBX
CMP RBX, 0
JNE OBLICZPODZIELPRZEZSUME
JMP OBLICZKONIECKONIEC
OBLICZPODZIELPRZEZSUME:
PXOR XMM0, XMM0	; Dzielimy przez sumę masek, jeśli różna od zera
CVTSI2SS XMM0, RAX	;! Wykorzystane instrukcje wektorowe: PXOR (MMX), DIVSS (SSE), CVTSI2SS (SSE), CVTTSS2SI (SSE)
PXOR XMM1, XMM1
CVTSI2SS XMM1, RBX
DIVSS XMM0, XMM1
CVTTSS2SI RAX, XMM0
OBLICZKONIECKONIEC:
POP R11	; przywracamy stos
POP R10
POP R9
POP R8
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
XOR R8, R8
XOR R9, R9
JMP STARTGLOWNEJPETLI
STARTGLOWNEJPETLI:
MOV R8, QWORD PTR [RSP+64]	; R8 = i
GLOWNAPETLA:		
MOV R9, QWORD PTR [RSP+56]
CMP R8, R9	; pierwszy rząd bitmapy (pomijamy)
JL KONIECGLOWNEJPETLI
MOV RAX, R8	; lewa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
XOR RDX, RDX
MOV RCX, QWORD PTR [RSP+56]
DIV RCX
CMP RDX, 0
JE KONIECGLOWNEJPETLI
MOV RCX, QWORD PTR [RSP+48] ; ostatni rząd bitmapy (pomijamy)
SUB RCX, QWORD PTR [RSP+56]
CMP R8, RCX
JGE KONIECGLOWNEJPETLI
MOV RAX, R8		; prawa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX (pomijamy)
ADD RAX, 2
INC RAX
XOR RDX, RDX
MOV RCX, QWORD PTR [RSP+56]
DIV RCX
CMP RDX, 0	
JE KONIECGLOWNEJPETLI
XOR R9, R9
PETLAZEWNETRZNA:		; R9 = y
XOR R10, R10
CMP R9, 3
JE KONIECPODWOJNEJPETLI
JMP PETLAWEWNETRZNA
PETLAWEWNETRZNA:		; R10 = x
MOV RCX, R10
DEC RCX
IMUL RCX, 3
MOV RAX, R9
DEC RAX
IMUL RAX, QWORD PTR [RSP+56]	; szerokosc bitmapy
ADD RCX, RAX
ADD RCX, R8
MOV RDX, R9
IMUL RDX, 3
ADD RDX, R10
MOV AL, BYTE PTR [R11 + RCX]	; R11 - wskaźnik na wejściową tablicę, RCX zawiera obliczony indeks piksela
MOV BYTE PTR [R13 + RDX], AL	; R13 - wskaźnik na tablicę R, RDX zawiera obliczony indeks w tej tablicy
INC RCX	
MOV AL, BYTE PTR [R11 + RCX]
MOV BYTE PTR [R14 + RDX], AL	; R14 - wskaźnik na tablicę G
INC RCX
MOV AL, BYTE PTR [R11 + RCX]
MOV BYTE PTR [R15 + RDX], AL	; R15 - wskaźnik na tablicę B
INC R10
CMP R10, 3
JNE PETLAWEWNETRZNA
INC R9
JMP PETLAZEWNETRZNA
KONIECPODWOJNEJPETLI:	; wartości zwracane z procedury ObliczNowaWartoscPiksela znajdują się w dolnym bajcie rejestru RAX (->AL)
MOV RCX, R13
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, QWORD PTR [RSP+64]
MOV BYTE PTR [R12 + RDX], AL
MOV RCX, R14
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, QWORD PTR [RSP+64]
INC RDX
MOV BYTE PTR [R12 + RDX], AL
MOV RCX, R15
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, QWORD PTR [RSP+64]
INC RDX
INC RDX
MOV BYTE PTR [R12 + RDX], AL
JMP KONIECGLOWNEJPETLI
KONIECGLOWNEJPETLI:
ADD R8, 3
MOV RAX, QWORD PTR [RSP+64]
ADD RAX, QWORD PTR [RSP+72]
CMP R8, RAX
JL GLOWNAPETLA
JMP KONIEC
KONIEC:
;MOV RAX, WskaznikNaWyjsciowaTablice ; w przypadku gdybyśmy chcieli zwrócić wskaźnik na tablicę wyjściową, nie ma jednak takiej potrzeby
XOR RAX, RAX	; upewniamy się, że nie ma wartości zwracanej (w przypadku void)
RET

NalozFiltrAsm ENDP

END
;-------------------------------------------------------------------------
