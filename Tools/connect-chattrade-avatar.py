"""Compatibility launcher for the packaged Avatar sign-in flow."""
import sys,os
os.environ.setdefault('DINGOUS_ENVIRONMENT','development')
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tracker'))
from chat_trade_pairing import main
if __name__=='__main__':main()
