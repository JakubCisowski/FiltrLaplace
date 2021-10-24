;-------------------------------------------------------------------------
.CODE

DllEntry PROC hInstDLL:DWORD, reason:DWORD, reserved1:DWORD
mov	eax, 1 
ret
DllEntry ENDP

NalozFiltrAsm proc bitmapaTablicaBajtow: DWORD, dlugoscBitmapy: DWORD, indeksStartowy: DWORD, ileIndeksowFiltrowac: DWORD
mov	EAX, 1
; poki co puste
ret
NalozFiltrAsm endp

END
;-------------------------------------------------------------------------
