# Sondagem auxiliar de escoamento

Compara apenas as passagens estáticas da V4 e V5 em MuJoCo, usando as exportações de colisores geradas pelo código no ambiente de teste. Não executa Unity, rotores, cruz lançadora, assistência, gatilhos de retorno ou recuperação. Serve para reproduzir os gargalos e conferir se a nova geometria escoa por gravidade.

Reprodução opcional para desenvolvimento, com Python:

```sh
python -m pip install mujoco==4.0.1 numpy==2.5.5
python probe_gravity_flow.py --assert
```

Pode ser executado de qualquer pasta, passando o caminho do script. Os arquivos JSON da mesma pasta contêm as duas geometrias e os resultados. A opção `--assert` exige que todos os seis casos da V5 escoem, sem saída indevida, na janela de 12 s. A simulação usa MuJoCo, com contatos diferentes de PhysX; uma partida completa continua dependendo do teste no Editor. Esses pacotes Python não são dependências do jogo Unity.
