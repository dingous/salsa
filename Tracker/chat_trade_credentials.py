"""Current-Windows-user DPAPI storage for an existing ChatTrade API session."""
import os,ctypes
from ctypes import wintypes
from pathlib import Path
FILE=Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'Dingous/chat-trade-token.dpapi'
if os.getenv('DINGOUS_ENVIRONMENT')=='production':FILE=FILE.with_name('chat-trade-production-token.dpapi')
class Blob(ctypes.Structure):
    _fields_=[('size',wintypes.DWORD),('data',ctypes.POINTER(ctypes.c_ubyte))]
def transform(value,encrypt):
    if os.name!='nt':raise RuntimeError('Credencial local protegida exige Windows.')
    buffer=ctypes.create_string_buffer(value);source=Blob(len(value),ctypes.cast(buffer,ctypes.POINTER(ctypes.c_ubyte)));target=Blob()
    crypt=ctypes.WinDLL('crypt32',use_last_error=True);kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.LocalFree.argtypes=[ctypes.c_void_p];kernel.LocalFree.restype=ctypes.c_void_p
    if encrypt:
        fn=crypt.CryptProtectData;fn.argtypes=[ctypes.POINTER(Blob),wintypes.LPCWSTR,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_void_p,wintypes.DWORD,ctypes.POINTER(Blob)]
        ok=fn(ctypes.byref(source),'Dingous ChatTrade session',None,None,None,1,ctypes.byref(target))
    else:
        fn=crypt.CryptUnprotectData;fn.argtypes=[ctypes.POINTER(Blob),ctypes.c_void_p,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_void_p,wintypes.DWORD,ctypes.POINTER(Blob)]
        ok=fn(ctypes.byref(source),None,None,None,None,1,ctypes.byref(target))
    if not ok:raise RuntimeError('O Windows não conseguiu acessar a credencial protegida deste usuário.')
    try:return ctypes.string_at(target.data,target.size)
    finally:kernel.LocalFree(target.data)
def load():
    if not FILE.exists():return ''
    return transform(FILE.read_bytes(),False).decode('utf-8')
def save(token):
    if not isinstance(token,str) or not 10<len(token)<32000:raise ValueError('Token inválido.')
    FILE.parent.mkdir(parents=True,exist_ok=True)
    temporary=FILE.with_suffix('.tmp');temporary.write_bytes(transform(token.encode('utf-8'),True));temporary.replace(FILE)
