# Basquete — bola livre e arremesso ao entrar no garrafão

[**Baixar a atualização mais recente**](ATUALIZACAO_BASQUETE_BOLA_LIVRE.zip)

Mecânica revisada conforme o pedido: **discos e bola com física semelhante à do futebol; o disco entra no garrafão adversário com a bola próxima e faz o arremesso automático**.

## O que mudou

- Bola solta, batidas, colisões e disputas. Ela não fica presa ao disco e não há sequência programada de posse e passes.
- O disco precisa estar no **garrafão do lado que está atacando**, com a bola ao alcance. A bola entrar sozinha, ou o disco entrar sem ela, não inicia um arremesso.
- Uma breve confirmação de proximidade evita arremessos por uma passagem instantânea. Se um defensor estiver mais perto da bola, o atacante precisa recuperar a disputa.
- O arremesso segue um arco até o aro. Acerto soma **2 pontos**; erro devolve a bola à física para disputar o rebote.
- Os arremessos de **3 pontos são opcionais e começam desligados**, para respeitar o gatilho do garrafão. Se você ligar a opção, um disco com a bola próxima fora da linha pode tentar de longe; os pontos usam a posição real do arremesso.
- Retirado o relógio de posse. A transmissão mostra `FREE BALL` e `ENTER THE PAINT WITH THE BALL`, em inglês.
- Especial mantido somente nas duas cenas de capitães: contatos distintos carregam a barra; a roleta é usada no próximo arremesso elegível.
- Permanecem o placar, os sons, a música, a apresentação final, a liga/mata-mata e a prorrogação. Um arremesso já iniciado pode terminar após o buzzer.

## Instalação

O ZIP contém **somente arquivos novos ou alterados**, de forma cumulativa. Pode ser aplicado na cópia de basquete que já recebeu a atualização anterior ou na cópia do último backup enviado. Não inclui o projeto completo.

1. Saia do Play e feche o Unity.
2. Extraia o ZIP e copie a pasta `Assets` para a raiz da **cópia destinada ao basquete**, junto de `Packages` e `ProjectSettings`.
3. **Mescle as pastas e substitua os arquivos de mesmo nome**, incluindo os `.meta`. Preserve os demais arquivos do seu projeto.
4. Reabra no Unity **6000.4.11f1**, aguarde a compilação e abra uma das quatro cenas abaixo.
5. No manager, use **Validar configuração** e depois Play.

As quatro cenas do pacote substituem as versões baseadas no backup. Os caminhos e GUIDs permanecem os mesmos:

| Cena | Especial | Configuração inicial |
| --- | --- | --- |
| `Assets/Scenes/FutebolDiscos/Liga_Discos.unity` | Não | 3 jogadores por time |
| `Assets/Scenes/FutebolDiscos/MataMata_Discos.unity` | Não | 3 jogadores por time |
| `Assets/Scenes/FutebolDiscos/Capitaes/Liga_Capitaes.unity` | Sim | 2 jogadores + 1 capitão por time |
| `Assets/Scenes/FutebolDiscos/Capitaes/MataMata_Capitaes.unity` | Sim | 2 jogadores + 1 capitão por time |

As fichas, fotos, nomes e escudos que você cadastrou continuam sendo usados. Nas cenas normais, escolha o capitão pelo nome e a função dos jogadores na ficha. As vagas restantes sem nome mostram a abreviatura e o número do disco. Nas cenas de convidados, o capitão usa a própria ficha.

## Ajustes no Inspector

Selecione `FutebolDiscos_Manager`. Em **Bola livre e arremesso automático**:

- **Alcance extra para pegar a bola:** aumenta ou diminui a tolerância de proximidade. Inicial: `0.35` unidade além dos raios do disco e da bola.
- **Tempo junto da bola para arremessar:** rapidez de confirmação ao entrar no garrafão. Inicial: `0.12 s`.
- **Força das batidas na bola** e **Velocidade máxima da bola:** controlam o ritmo físico, como no futebol.
- **Voo do arremesso**, **Pausa de reposição** e **Prorrogação:** duração das etapas.
- **Permitir arremessos automáticos de 3:** opção independente; desligada inicialmente.

A duração inicial continua em 120 segundos, com prorrogações de 20 segundos. Os controles de música e efeitos permanecem no mesmo painel. Há novos identificadores `basquete_livre_*` e temporada `bola_livre_01`, separados das temporadas anteriores.

## Validação

Foram usados testes C# com os arquivos reais do jogo e substitutos das APIs Unity para conferir gatilho, atores, bola, pontuação, especial, encerramento, rebote, HUD, campos do Inspector e quadra. Também foram verificados os quatro arquivos de cena e a aplicação cumulativa do ZIP.

**O Unity Editor não está disponível neste ambiente. A sensação das colisões, o visual e a execução em Play Mode ainda precisam de conferência no seu Unity.**
