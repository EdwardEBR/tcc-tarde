using UnityEngine;

/// <summary>Libera e executa a ação de sentar na cadeira do nível 2.</summary>
public class CadeiraSentavelInteractable : MonoBehaviour
{
    [Header("Altura do assento")]
    [Range(0.2f, 0.8f)] public float alturaRelativaDoAssento = 0.45f;
    public float rotacaoSentado;

    private Outline[] outlines;
    private bool liberada;
    private bool usada;

    private void Awake()
    {
        outlines = GetComponentsInChildren<Outline>(true);
        if (outlines.Length == 0)
            outlines = new[] { gameObject.AddComponent<Outline>() };
        DesativarOutlines();
    }

    public void Liberar()
    {
        liberada = true;
    }

    public void SetHighlight(bool ativo)
    {
        bool destacar = ativo && liberada && !usada;
        foreach (Outline outline in outlines)
        {
            if (outline != null) outline.enabled = destacar;
        }
    }

    public void DesativarOutlines()
    {
        if (outlines == null) outlines = GetComponentsInChildren<Outline>(true);
        foreach (Outline outline in outlines)
        {
            if (outline == null) continue;
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = new Color(1f, 0.45336083f, 0f, 1f);
            outline.OutlineWidth = 10f;
            outline.enabled = false;
        }
    }

    public void RestaurarColliders()
    {
        foreach (Collider colisorCadeira in GetComponentsInChildren<Collider>(true))
        {
            if (colisorCadeira != null) colisorCadeira.enabled = true;
        }
    }

    public void Interact(PlayerFPS jogador)
    {
        if (!liberada || usada || jogador == null) return;

        usada = true;
        SetHighlight(false);

        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length > 0) collider = colliders[0];
        }

        if (collider == null)
        {
            Debug.LogWarning("CadeiraSentavelInteractable: a cadeira não tem Collider para calcular o assento.", this);
            usada = false;
            return;
        }

        Bounds limites = collider.bounds;

        float alturaAssento = limites.min.y + limites.size.y * alturaRelativaDoAssento;
        Vector3 posicaoFinal = new Vector3(
            limites.center.x,
            alturaAssento + jogador.MeiaAlturaMundialDoPersonagem,
            limites.center.z);
        Vector3 frenteDaCadeira = transform.right;
        frenteDaCadeira.y = 0f;
        if (frenteDaCadeira.sqrMagnitude < 0.001f)
            frenteDaCadeira = transform.forward;
        Quaternion rotacaoFinal = Quaternion.LookRotation(frenteDaCadeira.normalized, Vector3.up)
            * Quaternion.Euler(0f, rotacaoSentado, 0f);
        jogador.SentarNaCadeira(posicaoFinal, rotacaoFinal);

        foreach (Collider colisorCadeira in GetComponentsInChildren<Collider>(true))
        {
            if (colisorCadeira != null) colisorCadeira.enabled = false;
        }
    }
}
