using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>Fluxo do prato empilhado após o jantar e pontos de entrega nas pias.</summary>
public class PlateStackKitchenInteractable : MonoBehaviour
{
    private static readonly Vector3 DestinoBanheiroNivel2 = new Vector3(2.20000005f, 3.79f, -29.7900009f);
    private static readonly List<Collider> collidersDasPias = new List<Collider>();

    private ItemSeguravel item;
    private bool liberado;
    private bool colocadoNaPia;

    public void Configurar(bool habilitado)
    {
        item = GetComponent<ItemSeguravel>();
        if (item == null) item = gameObject.AddComponent<ItemSeguravel>();
        item.nomeDoItem = "PlateStack_0";
        item.podeSerPego = habilitado;
        liberado = habilitado;

        Outline outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = new Color(1f, 0.45336083f, 0f, 1f);
        outline.OutlineWidth = 10f;
        outline.enabled = false;

        // O prefab PratoEmpilhado da cena não vem com collider; usa a malha raiz para o raycast.
        if (GetComponentInChildren<Collider>(true) == null)
        {
            MeshFilter malha = GetComponentInChildren<MeshFilter>(true);
            if (malha != null && malha.sharedMesh != null)
                malha.gameObject.AddComponent<MeshCollider>().sharedMesh = malha.sharedMesh;
        }

        int layer = LayerMask.NameToLayer("Interativel");
        if (layer >= 0)
            foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }

    public bool PodePegar => liberado && !colocadoNaPia && item != null && item.podeSerPego;

    public void AoPegar()
    {
        if (!PodePegar) return;
        PlayerFPS player = FindAnyObjectByType<PlayerFPS>();
        TaskManager tasks = FindAnyObjectByType<TaskManager>();
        if (tasks != null) tasks.ReproduzirTelaPretaFecharRapido();
        if (player != null) StartCoroutine(TeleportarDurantePiscada(player));
    }

    private IEnumerator TeleportarDurantePiscada(PlayerFPS player)
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (player != null) player.TeleportarPara(DestinoBanheiroNivel2);
    }

    public bool EncontrarPiaMirada(Ray raio, float distancia, out RaycastHit melhorAcerto)
    {
        bool encontrou = false;
        melhorAcerto = default;
        float menorDistancia = distancia;

        for (int i = collidersDasPias.Count - 1; i >= 0; i--)
        {
            Collider pia = collidersDasPias[i];
            if (pia == null)
            {
                collidersDasPias.RemoveAt(i);
                continue;
            }
            if (!pia.enabled || !pia.gameObject.activeInHierarchy) continue;

            if (pia.Raycast(raio, out RaycastHit acerto, menorDistancia))
            {
                menorDistancia = acerto.distance;
                melhorAcerto = acerto;
                encontrou = true;
            }
        }

        return encontrou;
    }

    public Outline ObterOutlinePia(Transform alvo)
    {
        Transform raiz = alvo;
        while (raiz != null && !EhNomeDePia(raiz.name)) raiz = raiz.parent;
        if (raiz == null) return null;

        Transform alvoDoOutline = alvo.GetComponent<Renderer>() != null ? alvo : raiz;
        Outline outline = alvoDoOutline.GetComponent<Outline>();
        if (outline == null) outline = alvoDoOutline.gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = new Color(1f, 0.45336083f, 0f, 1f);
        outline.OutlineWidth = 10f;
        return outline;
    }

    public void ColocarNaPia(ItemSeguravel prato, Vector3 ponto)
    {
        if (prato == null) return;
        prato.SoltarItem();
        prato.transform.position = ponto;
        prato.podeSerPego = false;
        liberado = false;
        colocadoNaPia = true;
        foreach (Collider colisor in prato.GetComponentsInChildren<Collider>(true))
            if (colisor != null) colisor.enabled = false;
        Outline outlinePrato = prato.GetComponent<Outline>();
        if (outlinePrato != null) outlinePrato.enabled = false;
        prato.gameObject.SetActive(false);
    }

    public static void PrepararPias()
    {
        collidersDasPias.Clear();
        int layer = LayerMask.NameToLayer("Interativel");
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform objeto in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!EhNomeDePia(objeto.name)) continue;
                    if (layer >= 0)
                        foreach (Transform filho in objeto.GetComponentsInChildren<Transform>(true))
                            filho.gameObject.layer = layer;

                    // Garante collider em cada malha importada que ainda não o tenha.
                    MeshFilter[] malhas = objeto.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter malha in malhas)
                    {
                        if (malha == null || malha.sharedMesh == null
                            || malha.GetComponent<Collider>() != null) continue;

                        MeshCollider colisor = malha.gameObject.AddComponent<MeshCollider>();
                        colisor.sharedMesh = malha.sharedMesh;
                    }

                    foreach (Collider colisor in objeto.GetComponentsInChildren<Collider>(true))
                        if (colisor != null && !collidersDasPias.Contains(colisor)) collidersDasPias.Add(colisor);
                }
            }
        }
        Physics.SyncTransforms();
    }

    private static bool EhNomeDePia(string nome)
    {
        return string.Equals(nome, "KitchenSink_0", System.StringComparison.OrdinalIgnoreCase);
    }
}
