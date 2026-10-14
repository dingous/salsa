# Contrato de rig Dingous

Cartoon e Corporativo usam o mesmo controlador em cada plataforma. Webcam e Parceiro são fontes de atuação; não devem criar variantes do solver. O laboratório injeta dados pelo mesmo caminho de pose/expressões. Os modelos têm proporções e canais faciais próprios: compartilhar controle não cria morph targets ausentes.

Requisitos compartilhados: IK analítico de dois ossos por braço, alvo proporcional à largura dos ombros, âncora de cabeça para gestos perto do rosto, limite de cotovelo 150°, direção de dobra suavizada antes do IK, punho orientado pela palma, dedos transportados com a rotação da palma. Nunca mover/escalar os pontos de ligação dos ossos para atingir a mão.

Filtros One Euro: corpo cutoff 1.2 / ganho .65; mãos cutoff 1.5 / ganho 3. Retenção individual de ponto até 150 ms, pose local de mãos até 180 ms; soltura progressiva 4/s. Angular máximo braço 360°/s e antebraço 720°/s. Coordenadas de profundidade diferem entre Unity e Three.js conforme posição da câmera; a anatomia/esquerda-direita segue o espelhamento da fonte.

Windows limpa a fonte e a fila de pacotes ao entrar no Parceiro, preservando decaimento dos coeficientes faciais. A prévia/voz não transforma webcam residual em movimento do parceiro. Expressões continuam usando os canais realmente presentes no modelo; testes escolhem canais por capacidade, não por nome do cenário.

Verificação: node Web/test-rig.mjs executa 60 verificações matemáticas e de controlador com esqueletos sintéticos de duas proporções e ambos espelhamentos. Windows --corporate-smoke --rig-parity executa a matriz Cartoon/Corporativo × Webcam/Parceiro nos modelos VRM reais, com dados de atuação sintéticos: 459 PASS na execução final. Cobre alcance, dedos, espelhamento, cabeça, expressões, pernas, torso, posição dos ossos e geometria limitada. Não é E2E físico de webcam nem avaliação visual de toda colisão/roupa; web ainda exige validação visual no navegador. Nenhuma alegação de rig perfeito sob qualquer condição.
