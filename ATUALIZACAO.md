# Interface somente em inglês — liga Pokémon

O ZIP `Downloads/Pokemon_Liga_Apenas_Ingles_2026-11-11.zip` atualiza os textos da versão `pokemon-liga-v2026.11.11.3`, na qual o tamanho das bolas já foi corrigido.

1. Saia do Play, salve sua cena e feche o Unity.
2. Extraia o ZIP de idioma.
3. Copie a pasta **Assets** do ZIP para dentro da pasta **Projeto**, substituindo os arquivos correspondentes.
4. Abra novamente no Unity **6000.4.11f1** e aguarde a compilação.

O ZIP contém `ClassificacaoUI.cs`, `AberturaJogoUI.cs`, `TournamentManager.cs` e seus `.meta`. Os GUIDs existentes são preservados. A cena, as configurações, os sprites das bolas e as classificações salvas continuam iguais. Você pode retomar a mesma temporada.

Os textos exibidos passam a ser:

- `STANDINGS`, `TEAM`, `RESULTS` e `FIXTURES`.
- `BEFORE MATCHDAY` e `AFTER MATCHDAY`.
- `MATCHDAY`, `MATCH` e `MATCHDAY … COMPLETE`.

Esta atualização altera apenas textos exibidos. Os controles do Inspector e as instruções de uso continuam em português. O nome da liga e os nomes dos participantes continuam usando o que você configurou.

O projeto completo `pokemon-liga-v2026.11.11.4` já inclui esta alteração e todas as correções anteriores. O ZIP pequeno de idioma deve ser aplicado sobre a versão .3.

Foi feita uma revisão dos textos e repetido o teste existente da interface, com 376 verificações aprovadas. O Unity Editor não foi executado neste ambiente; a aparência precisa ser conferida no Unity.
