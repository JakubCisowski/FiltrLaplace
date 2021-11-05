;-------------------------------------------------------------------------
.CODE

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
mov	eax, 1 
ret
DllEntry ENDP

NalozFiltrAsm proc bitmapaTablicaBajtow: DWORD, dlugoscBitmapy: DWORD, indeksStartowy: DWORD, ileIndeksowFiltrowac: DWORD
;-------------------------------------------------------------------------
; PARAMETRY FUNKCJI NAKLADAJACEJ FILTR LAPLACE W ASM:
; bitmapaTablicaBajtow	--->	tablica bajtow ktora przchowuje dane bitmapy
; dlugoscBitmapy		--->	oznacza rozmiar tablicy bajtow ktora jest bitmapa
; indeksStartowy		--->    indeks tablicy bajtow od ktorego algorytm zacznie filtrowanie obrazu
; ileIndeksowFiltrowac  --->    ilosc elementow ktore funkcja przefiltruje na podstawie algorytmu laplace
;-------------------------------------------------------------------------
movzx R8, BYTE PTR [RCX + 2] ; Sprawdzam trzeci element tablicy
mov RAX, R8					 ; Zapisuje go do rejestru RAX
ret
NalozFiltrAsm endp

END
;-------------------------------------------------------------------------
