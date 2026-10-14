"""Optional offline LoRA training from explicitly approved chat examples."""
import argparse,json
from pathlib import Path
def dataset(path):
 rows=[]
 for line in Path(path).read_text(encoding='utf-8').splitlines():
  if not line.strip():continue
  row=json.loads(line);messages=row.get('messages',[])
  if len(messages)!=2 or [m.get('role') for m in messages]!=['user','assistant'] or any(not isinstance(m.get('content'),str) or not m['content'].strip() for m in messages):raise ValueError('Exemplo inválido.')
  rows.append(row)
 if not rows:raise ValueError('Ainda não há exemplos aprovados para treinamento.')
 return rows
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--dataset',required=True);parser.add_argument('--output',default='training/dingous-adapter');parser.add_argument('--validate-only',action='store_true');args=parser.parse_args();rows=dataset(args.dataset)
 if args.validate_only:print(json.dumps({'examples':len(rows),'trained':False}));raise SystemExit(0)
 import torch
 from datasets import Dataset
 from transformers import AutoProcessor,AutoModelForMultimodalLM,BitsAndBytesConfig
 from peft import LoraConfig,prepare_model_for_kbit_training
 from trl import SFTConfig,SFTTrainer
 if not torch.cuda.is_available():raise RuntimeError('Use ambiente CUDA de treinamento separado; não execute no ambiente Tracker.')
 modelName='Qwen/Qwen3.5-4B'
 model=AutoModelForMultimodalLM.from_pretrained(modelName,quantization_config=BitsAndBytesConfig(load_in_4bit=True,bnb_4bit_quant_type='nf4',bnb_4bit_compute_dtype=torch.float16),device_map={'':0})
 model=prepare_model_for_kbit_training(model);model.config.use_cache=False
 processor=AutoProcessor.from_pretrained(modelName)
 config=SFTConfig(output_dir=args.output,per_device_train_batch_size=1,gradient_accumulation_steps=8,gradient_checkpointing=True,max_length=512,num_train_epochs=1,learning_rate=1e-4,fp16=True,report_to='none',save_strategy='epoch')
 trainer=SFTTrainer(model=model,args=config,train_dataset=Dataset.from_list(rows),processing_class=processor,peft_config=LoraConfig(r=8,lora_alpha=16,lora_dropout=.05,target_modules=['q_proj','v_proj','o_proj'],task_type='CAUSAL_LM'))
 trainer.train();trainer.save_model(args.output);processor.save_pretrained(args.output)
 print('Adaptador salvo. Avalie antes de converter/importar ou substituir o modelo Ollama.')
