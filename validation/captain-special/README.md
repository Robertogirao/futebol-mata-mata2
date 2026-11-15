# Barra e roleta dos capitães

Execute com Python 3 e um SDK .NET 8 ou mais recente:

```sh
bash validation/captain-special/run.sh
```

Se necessário, defina `DOTNET=/caminho/para/dotnet`. `TEST_SOURCE` pode apontar para outra pasta de projeto Unity com a mesma mecânica. O runner compila os scripts reais de `Projeto/Assets` e a facade Unity/TMP já usada pelas regressões do repositório. Não baixa pacotes NuGet, não altera o projeto e usa uma pasta temporária para os binários.

Por padrão executa a suíte nova e as regressões de partida, reposição após gol, capitães e bola presa no canto. Para executar somente a nova suíte:

```sh
bash validation/captain-special/run.sh FeatureHarness
```

A suíte nova verifica contatos contínuos contados uma vez, recontatos rápidos, atribuição de cobrança sem carga, capitães independentes, limite de cinco toques, acerto/erro, pontuação única, fim e reinício da partida, probabilidades por overall, áreas da malha da roleta, barra e ponteiro nas fichas externas.

**Limite:** são testes dos scripts reais contra uma facade de API em C#, com callbacks injetados. Não executam o editor Unity, o solver nativo de física, a renderização TMP ou a GPU. Uma verificação visual e de colisões dentro do Unity continua sendo complementar.
