# Sondagem da cruz em movimento

Compara V5 e V6 em MuJoCo 4.0.1, usando a geometria dos colisores gerada pelo código no ambiente de testes. A cruz é representada por um corpo com articulação, posição e velocidade angular prescritas a cada passo, com armadura elevada para aproximar um motor cinemático. Usa a rotação nominal de cada versão; não reproduz a oscilação de RPM.

São 32 quedas independentes: quatro posições de entrada e oito fases diferentes entre duas pás consecutivas. Inclui a canaleta e as guias estáticas. Não inclui assistência de arremesso, outros jogadores, aro, tabela ou integração PhysX.

Reprodução opcional:

```sh
python -m pip install mujoco==4.0.1 numpy==2.5.5
python probe_launcher.py --assert
```

Os arquivos da mesma pasta contêm geometrias e resultados. As métricas são:

- **contacts:** houve contato físico da esfera com a cruz.
- **up_forward:** 40 ms após o último contato, a velocidade vertical supera 1,5 m/s e a componente em direção à cesta supera 0,8 m/s.
- **rim_height:** o arco ideal dessa saída sobe à altura do aro mais o raio da esfera, com movimento em direção à cesta.
- **near_rim:** extrapolação balística da saída cruza a altura do aro a menos de 1,5 m do centro. Essa distância é maior que a abertura do aro; não significa cesta.

A V6 deve registrar pelo menos 24 contatos em 32 quedas, mais saídas ascendentes que a V5, pelo menos 12 saídas ascendentes, quatro arcos com altura suficiente e uma trajetória próxima. O resultado obtido foi 27 contatos, 17 saídas ascendentes, 10 arcos com altura suficiente e seis trajetórias próximas. Esses valores não representam probabilidades de cesta no Unity. O jogo mantém a ajuda leve em 0,25, embora a sondagem a deixe desativada para avaliar o mecanismo.
