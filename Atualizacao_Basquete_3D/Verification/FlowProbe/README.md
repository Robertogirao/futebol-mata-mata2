# Sondagem auxiliar de escoamento

Compara as passagens estáticas da V4, V5 e V6 em MuJoCo, usando colisores gerados pelo código no ambiente de teste. Não executa Unity, rotores, cruz lançadora, assistência, gatilhos de retorno ou recuperação. Avalia se a geometria escoa por gravidade sem depender desses mecanismos.

Reprodução opcional:

```sh
python -m pip install mujoco==4.0.1 numpy==2.5.5
python probe_gravity_flow.py --assert
```

O script pode ser executado de qualquer pasta, passando seu caminho. Os JSONs da mesma pasta contêm as três geometrias e os resultados. A opção `--assert` exige que os seis casos da V6 escoem sem saída indevida na janela de 12 s. O motor de contato é diferente de PhysX. Esses pacotes Python são usados apenas para verificação externa, sem dependência no jogo Unity.
