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
}
