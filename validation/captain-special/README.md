# Barra e roleta dos capitães

Execute com Python 3 e um SDK .NET 8 ou mais recente:

```sh
bash validation/captain-special/run.sh
```

Se necessário, defina `DOTNET=/caminho/para/dotnet`. `TEST_SOURCE` pode apontar para outra pasta de projeto Unity com a mesma mecânica. O runner compila os scripts reais de `Projeto/Assets` e a facade Unity/TMP já usada pelas regressões do repositório. Não baixa pacotes NuGet, não altera o projeto e usa uma pasta temporária para os binários.

Por padrão executa a suíte nova e as regressões de partida, reposição após gol, capitães, bola presa no canto, capitães convidados e HUD. Para executar somente a nova suíte:

```sh
bash validation/captain-special/run.sh FeatureHarness
```

Nesta revisão, a roleta ganha anéis, marcações, face sombreada e ponteiro com detalhes. Gols especiais concluídos entram na fila com uma comemoração própria (`SPECIAL GOAL!`), enquanto gols comuns mantêm a aparência normal.

Os painéis laterais ficam largos e espelhados, deixando livre o rodapé central. O rodapé usa apenas os textos personalizados habilitados, sem adicionar formato da competição ou contagem de discos.

O voo HIT dura 1,4 s e o MISS dura 0,9 s. O fundo do painel pulsa lentamente em um ciclo de 2,6 s somente enquanto um resultado está armado esperando o próximo toque, mantendo alpha 1.

A suíte nova verifica contatos contínuos contados uma vez, recontatos rápidos, atribuição de cobrança sem carga, capitães independentes, limite de cinco toques, resultado HIT/MISS armado exatamente ao parar a roleta, espera pelo próximo contato distinto, voo animado da bola, pontuação única, cancelamento durante o voo, restauração de escalas/rastro, fim e reinício da partida, probabilidades por overall, áreas/alpha da malha, Image e CanvasRenderer, textura branca, painel opaco, pulso suave de espera (base, pico aos 1,3 s e retorno aos 2,6 s), interrupção do pulso no próximo toque, retângulos largos espelhados sem sobreposição ao campo/retratos/rodapé, rodapé só com texto personalizado em todas as combinações de flags, áreas reais dos setores coloridos da roleta sem contar a decoração, fila normal–especial–normal com identidade e estilo independentes, limpeza de efeitos/cancelamento/reconstrução, duração em captura 30/60 fps e `timeScale = 0`, texto em inglês e barra configurável com preenchimento suave e brilho móvel.

**Limite:** são testes dos scripts reais contra uma facade de API em C#, com callbacks injetados. Não executam o editor Unity, o solver nativo de física, a renderização TMP ou a GPU. Uma verificação visual e de colisões dentro do Unity continua sendo complementar.
