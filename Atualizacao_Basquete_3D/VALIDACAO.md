# Validação da V5 — cantos, descida e retorno correto

Versão alvo: Unity **6000.4.11f1**. O ambiente de desenvolvimento não possui Editor Unity instalado.

O relato da V4 mostrou bolinhas presas nos cantos frontais e na garganta do funil superior. Também foi solicitado que o coletor sob a cesta devolvesse cada bola ao reservatório do próprio time. A V5 corrige essas passagens e esse destino, mantendo a cruz contínua e a assistência leve.

## Verificações de código executadas

- **7.232 verificações em C# puro:** tempo, pausa, reinício, morte súbita e validação de cesta sem duplicação.
- **1.307 verificações com substitutos das APIs Unity:** mecanismos, identidade das oito bolas, assistência leve, contatos repetidos, retorno ao reservatório correto para vermelho/azul e para cestas/erros, duração do retorno, rejeição de transportes duplicados e recuperação de passagem bloqueada mesmo com tremores locais.
- Os testes distinguem falta de avanço, avanço lento real e disputa ativa na área central dos rotores. O reinício limpa os prazos de recuperação.
- **12 grupos estruturais:** sintaxe, GUIDs, assemblies, cena, prefab, configurações, folgas da descida e independência do futebol antigo.
- Compilação dos scripts de execução e de Editor contra substitutos das dependências. **Não equivale a compilação contra as bibliotecas reais do Unity ou execução de PhysX.**
- Regras de pontuação, identidade, evento de resultado, cruz contínua e cálculo da ajuda leve permanecem preservados. A mudança do retorno afeta destino e caminhos visuais; não cria outra bola nem outra tentativa.

## Reprodução física auxiliar do problema

Foi usado **MuJoCo 4.0.1**, com a geometria dos colisores gerada pelo código e exportada no ambiente de testes. Gravidade de 9,81 m/s², esferas de massa 0,45 kg, atrito 0,08, passo de 0,002 s e janela de 12 s. Motores, assistência de arremesso, gatilhos e recuperação programada ficaram fora dessa sondagem: o objetivo foi avaliar o escoamento passivo das passagens.

A mesma sondagem comparou V4 e V5:

| Caso | V4: bolas que passaram | V5: bolas que passaram | Última passagem na V5 |
|---|---:|---:|---:|
| Canto frontal esquerdo | 0/1 | 1/1 | 5,742 s |
| Canto frontal direito | 0/1 | 1/1 | 5,572 s |
| Duas juntas na descida | 0/2 | 2/2 | 0,952 s |
| Oito na frente da plataforma | 0/8 | 8/8 | 3,336 s |
| Duas no coletor sob a cesta | 0/2 | 2/2 | 0,574 s |
| Oito na frente, raio máximo de 0,22 m | 0/8 | 8/8 | 3,566 s |

Os seis casos da V4 permaneceram retidos na janela observada. Na V5, as 22 esferas dos casos atravessaram as respectivas passagens, sem recuperação por script. Para o topo, passagem significa atingir y < 2,8 m na área da roda; para o coletor, y < 0,1 m sob a abertura. Isso não mede cestas nem representa uma partida inteira.

O material para repetir a sondagem está em **Verification/FlowProbe**: script, duas exportações e resultados. Esse motor tem contato e integração diferentes de PhysX; os resultados ajudam a identificar o gargalo geométrico, mas não substituem a validação no Unity.

## Testes nativos fornecidos para o Editor

Em **Window → General → Test Runner**:

- **EditMode:** regras e limite da assistência leve.
- **PlayMode:** partida completa, oito identidades, oportunidades dos dois times, rotação contínua, pausa/recuperação e uma regressão específica com duas bolas nos cantos e duas na descida. Esse teste exige chegada à área da roda sem aceitar transporte de recuperação como sucesso.

Esses testes nativos não foram executados aqui. A sondagem externa também não determina uma taxa de acerto da V5.

## Conferência manual

1. Abrir a cena, entrar em Play e conferir quatro bolas por equipe.
2. Observar os cantos diagonais e a descida aberta: o placar de arremessos deve começar a subir por contato com a cruz.
3. Conferir que o retorno sob a cesta segue pelos ramos e elevadores laterais até o reservatório do próprio time, sem atalho para a cruz.
4. Executar uma partida inteira e conferir cestas, erros, oito identidades, resultado, pausa e reinício.

A assistência segue em 0,25, aplicada uma única vez por impacto. Esse número limita a correção de velocidade; não é uma porcentagem de acertos.
