"""Unity explicitly selects the environment; public builds never fall back to local services."""
import os

def endpoints():
    development=os.getenv('DINGOUS_ENVIRONMENT','production').lower()=='development'
    return ('http://127.0.0.1:5100','http://localhost:6300') if development else ('https://dingous.com.br','https://dingous.com.br')
