# Requisito de rastreamento

Mãos, corpo e expressões são requisitos centrais do usuário. Nenhuma dessas partes deve ser omitida da animação do avatar.

## Cobertura implementada

- Cabeça, olhar, pálpebras, sobrancelhas, bochechas, nariz, lábios e maxilar.
- Duas mãos, pulsos e dez dedos articulados.
- Quadril, coluna, ombros, braços, coxas, joelhos, tornozelos e pés.
- Espelhamento, calibração da cabeça, suavização e retorno ao repouso.
- Indicação de captura parcial e rejeição de coordenadas inválidas.

## Validação

Testes automatizados verificam deformação não vazia nos 52 controles faciais,
reação de todos os dez dedos, movimento de coluna e pernas, e recuperação após
perda de rastreamento. Esses testes usam landmarks sintéticos para conhecer o
movimento esperado. A comunicação com os modelos reais também foi executada.

Ainda é necessário validar movimentos humanos em diferentes enquadramentos,
iluminações, cruzamento de braços, mãos sobrepostas e gestos rápidos. Não existe
medição de erro contra um sistema de captura de referência.

## Limite da exigência de 100%

Cobertura dos controles não equivale a precisão de captura de 100%. Uma webcam
monocular não observa pontos ocultos ou fora do quadro. O MediaPipe utilizado
também não estima língua, apesar de o rig oferecer um controle para fontes
externas. Não declarar que a precisão ou a captura de todas as expressões é
100% sem evidências e um critério de referência mensurável.
