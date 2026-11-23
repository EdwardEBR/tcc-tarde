using System;
using UnityEngine;

[Serializable]
public class NoConversa
{
    [TextArea(2, 5)] public string falaDaPessoa;
    public string resposta1;
    public int proximoNoResposta1 = -1;
    public string resposta2;
    public int proximoNoResposta2 = -1;
    public bool piscarTelaAntesDaFala;
    public bool falaAvancaAutomaticamente;
    public bool aguardarSentarAposFala;
    public bool eventoAutoAvanca;
    public bool transicaoTelaPretaRapida;
    [Min(0f)] public float duracaoLapso = 0.48f;
}
