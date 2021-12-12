;-------------------------------------------------------------------------
.DATA

; Zmienne

Maski QWORD 9 DUP (0)
WskaznikNaWejsciowaTablice QWORD 0
WskaznikNaWyjsciowaTablice QWORD 0
DlugoscBitmapy QWORD 0
SzerokoscBitmapy QWORD 0
IndeksStartowy QWORD 0
IleIndeksowFiltrowac QWORD 0

TablicaR QWORD 9 DUP (0)
TablicaG QWORD 9 DUP (0)
TablicaB QWORD 9 DUP (0)

.CODE

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
mov	eax, 1 
ret
DllEntry ENDP

SumujMaski PROC
; Procedura sumująca maski.

PUSH RCX
PUSH RDX
XOR RAX, RAX
MOV RDX, 9
LEA RCX, Maski
SUMUJMASKIPETLA:
ADD RAX, QWORD PTR [RCX]
DEC RDX
CMP RDX, 0
JE SUMUJMASKIKONIEC
ADD RCX, 8
JMP SUMUJMASKIPETLA
SUMUJMASKIKONIEC:
POP RDX
POP RCX
RET

SumujMaski ENDP

InicjalizujMaski PROC
; Procedura inicjalizująca maski.
; http://www.algorytm.org/przetwarzanie-obrazow/filtrowanie-obrazow.html - filtr LAPL1

PUSH RCX
LEA RCX, Maski
MOV QWORD PTR [RCX], 0
MOV QWORD PTR [RCX + 16], 0
MOV QWORD PTR [RCX + 48], 0
MOV QWORD PTR [RCX + 64], 0
MOV QWORD PTR [RCX + 8], -1
MOV QWORD PTR [RCX + 24], -1
MOV QWORD PTR [RCX + 40], -1
MOV QWORD PTR [RCX + 56], -1
MOV QWORD PTR [RCX + 32], 4
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
MOV R11, QWORD PTR [R9 + 8 * R10]
IMUL RDX, R11	
ADD RAX, RDX	; sumujemy wartość piksela
INC RCX
CMP RCX, 3
JNE OBLICZPETLAWEWN
INC RBX
JMP OBLICZPETLAZEWN
OBLICZKONIEC:
CALL Clamp
MOV R8, RAX
CALL SumujMaski
MOV RBX, RAX	; suma masek w RBX
MOV RAX, R8		; wartość piksela w RAX
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
; dlugoscBitmapy - zapisany do R8
; szerokoscBitmapy - zapisany do R9
; indeksStartowy - piąty parametr na stosie
; ileIndeksowFiltrowac - szósty parametr na stosie
; Procedura nie zwraca wyniku (wynik odczytywany jest za pomocą jednego ze wskaźników wyjściowych).

MOV WskaznikNaWejsciowaTablice, RCX
MOV WskaznikNaWyjsciowaTablice, RDX
MOV DlugoscBitmapy, R8
MOV SzerokoscBitmapy, R9
MOV RAX, QWORD PTR [RSP + 40]
MOV IndeksStartowy, RAX
MOV RAX, QWORD PTR [RSP + 48]
MOV IleIndeksowFiltrowac, RAX
XOR R8, R8	; czyścimy wszystkie rejestry
XOR R9, R9
XOR R10, R10
XOR R11, R11
XOR R12, R12
XOR R13, R13
XOR R14, R14
XOR R15, R15
JMP STARTGLOWNEJPETLI
STARTGLOWNEJPETLI:
CALL InicjalizujMaski
MOV R8, IndeksStartowy	; R8 = i
GLOWNAPETLA:		
MOV R9, SzerokoscBitmapy ; R9 = szerokość bimapy
CMP R8, R9	; pierwszy rząd bitmapy (pomijamy)
JL KONIECGLOWNEJPETLI
MOV RAX, R8	; lewa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX
XOR RDX, RDX
MOV RCX, SzerokoscBitmapy
DIV RCX
cmp RDX, 0
JE KONIECGLOWNEJPETLI
MOV RCX, DlugoscBitmapy ; ostatni rząd bitmapy
SUB RCX, SzerokoscBitmapy
CMP R8, RCX
JGE KONIECGLOWNEJPETLI
MOV RAX, R8		; prawa krawędź bitmapy - RAX = RAX / RCX, RDX = RAX % RCX
ADD RAX, 2
INC RAX
XOR RDX, RDX
MOV RCX, SzerokoscBitmapy
DIV RCX
CMP RDX, 0	
JE KONIECGLOWNEJPETLI
XOR R10, R10
PETLAZEWNETRZNA:		; R10 = y
XOR R11, R11
CMP R10, 3
JE KONIECPODWOJNEJPETLI
JMP PETLAWEWNETRZNA
PETLAWEWNETRZNA:		; R11 = x
MOV R12, R11
DEC R12
IMUL R12, 3
MOV RAX, R10
DEC RAX
IMUL RAX, SzerokoscBitmapy
ADD R12, RAX
ADD R12, R8
MOV R13, R10
IMUL R13, 3
ADD R13, R11
MOV RBX, WskaznikNaWejsciowaTablice
MOV R14B, BYTE PTR [RBX + R12]
LEA RAX, TablicaR
MOV BYTE PTR [RAX + R13], R14B
INC R12	
MOV RBX, WskaznikNaWejsciowaTablice
MOV R14B, BYTE PTR [RBX + R12]
LEA RAX, TablicaG
MOV BYTE PTR [RAX + R13], R14B
INC R12
MOV RBX, WskaznikNaWejsciowaTablice
MOV R14B, BYTE PTR [RBX + R12]
LEA RAX, TablicaB
MOV BYTE PTR [RAX + R13], R14B
INC R11
CMP R11, 3
JNE PETLAWEWNETRZNA
INC R10
JMP PETLAZEWNETRZNA
KONIECPODWOJNEJPETLI:	; wartości zwracane z procedury ObliczNowaWartoscPiksela znajdują się w dolnym bajcie rejestru RAX (->AL)
LEA RCX, TablicaR
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, IndeksStartowy
MOV R15B, AL
MOV RCX, WskaznikNaWyjsciowaTablice
MOV BYTE PTR [RCX + RDX], R15B
LEA RCX, TablicaG
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, IndeksStartowy
INC RDX
MOV R15B, AL
MOV RCX, WskaznikNaWyjsciowaTablice
MOV BYTE PTR [RCX + RDX], R15B
LEA RCX, TablicaB
CALL ObliczNowaWartoscPiksela
MOV RDX, R8
SUB RDX, IndeksStartowy
INC RDX
INC RDX
MOV R15B, AL
MOV RCX, WskaznikNaWyjsciowaTablice
MOV BYTE PTR [RCX + RDX], R15B
JMP KONIECGLOWNEJPETLI
KONIECGLOWNEJPETLI:
ADD R8, 3
MOV RAX, IndeksStartowy
ADD RAX, IleIndeksowFiltrowac
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
