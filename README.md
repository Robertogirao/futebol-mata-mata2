# Atualização do gerador Pokémon

[**Baixar ZIP de atualização**](https://github.com/Robertogirao/futebol-mata-mata2/raw/refs/tags/pokemon-v2026.11.11-gerador/Downloads/ATUALIZACAO_GERADOR_POKEMON.zip)

O pacote permite marcar várias gerações e vários tipos, escolher exatamente quantas cenas criar (padrão: **1**) e gerar novos confrontos a cada clique em **Gerar**, com histórico persistente para evitar repetições.

## Aplicar no projeto Pokémon

1. Feche o Unity e extraia o ZIP.
2. Copie a pasta **Assets** do pacote sobre a pasta **Assets** do projeto Pokémon, substituindo os arquivos correspondentes.
3. Reabra o Unity e aguarde a recompilação.
4. Use **Tools → Pokémon → Gerador de torneios**.

Use **Unity 6000.4.11f1**. O pacote contém seis scripts com seus arquivos `.meta`, instruções e relatório de validação. As cenas e os dados salvos do projeto são preservados.

A lógica foi compilada e testada em C# com substitutos das APIs Unity/UnityEditor, incluindo as 1.025 fichas reais do catálogo. A janela e o Play Mode ainda precisam ser conferidos no Unity.

SHA256 do ZIP: `7e384967fd1ad42171a4f83ded0553858f2b0bc2d7d791ca27a823b4b3f635fc`.
