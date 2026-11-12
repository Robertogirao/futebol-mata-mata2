# Tamanho visual da bola Pokémon

O sprite de substituição tinha recorte de 486 × 486 px e 100 pixels por unidade. Sua largura local era 4,86 unidades. Com a escala 0,65 do prefab, a bola era desenhada com 3,159 unidades e o aro com 3,4749. O colisor mantinha diâmetro físico de 0,65 unidade.

A correção usa uma cópia dedicada do mesmo PNG, sem alterar seus bytes, com 486 pixels por unidade. O recorte continua igual. O prefab passa a usar somente o GUID desse sprite novo: bola de 0,65 unidade, aro de 0,715 e retrato de no máximo 0,572.

A escala raiz, o colisor, os parâmetros de física, os scripts, a cena, o tema, a hélice e os logos foram preservados. O sprite `Nucleo_Pokebola` original continua com os mesmos PNG e `.meta`; ele tem outros usos na arena e no placar.

## Reexecutar

É necessário Python 3 e PyYAML. Mantenha `baseline.json` ao lado do script e o validador de referências na pasta irmã `prefab-repair`.

```sh
python3 validate_ball_scale.py /caminho/para/Projeto --output report.json
```

O baseline foi capturado do projeto no commit `39304b8fa604aeb931e6d717c0df7460d39e45b4`, antes da correção. O teste compara o prefab inteiro, os metadados, os bytes PNG, 1.075 fontes preservadas, GUIDs dos assets e referências locais de todas as cenas e prefabs.

Esta é uma validação dimensional e de serialização dos arquivos fonte. O Unity Editor não foi executado; a importação real, a aparência e o Play precisam ser conferidos no Unity 6000.4.11f1.
