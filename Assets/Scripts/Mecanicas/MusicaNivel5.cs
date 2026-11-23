using System.Collections;
using UnityEngine;

/// <summary>Controla a trilha do Nivel5 e seus fades de entrada e saída.</summary>
public sealed class MusicaNivel5 : MonoBehaviour
{
    private const string CaminhoDaMusica = "Music/grand_project-sad-melancholic-piano-forgotten-waltz-502650";
    private const string ChaveVolumeMusica = "MusicVolume";
    private const float AtrasoAposChegada = 3f;
    private const float DuracaoFadeEntrada = 4f;

    private static MusicaNivel5 instancia;
    private AudioSource fonte;
    private bool fadeEntradaEmAndamento;
    private bool fadeSaidaEmAndamento;

    public static bool FadeSaidaEmAndamento => instancia != null && instancia.fadeSaidaEmAndamento;

    public static void ManterSomApenasDaTrilhaFinal()
    {
        AudioSource[] fontes = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < fontes.Length; i++)
        {
            AudioSource fonte = fontes[i];
            if (fonte == null || (instancia != null && fonte == instancia.fonte)) continue;

            fonte.Stop();
            fonte.mute = true;
        }
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.volume = 0f;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        if (fonte != null && fonte.isPlaying && !fadeEntradaEmAndamento && !fadeSaidaEmAndamento)
            fonte.volume = VolumeMusicaConfigurado();
    }

    public void IniciarAposChegada()
    {
        StartCoroutine(AguardarEIniciar());
    }

    private IEnumerator AguardarEIniciar()
    {
        yield return new WaitForSecondsRealtime(AtrasoAposChegada);

        AudioClip faixa = Resources.Load<AudioClip>(CaminhoDaMusica);
        if (faixa == null)
        {
            Debug.LogWarning("MusicaNivel5: não foi possível carregar a trilha em Resources/" + CaminhoDaMusica, this);
            yield break;
        }

        fonte.clip = faixa;
        fonte.volume = 0f;
        fonte.Play();

        fadeEntradaEmAndamento = true;
        float tempo = 0f;
        while (tempo < DuracaoFadeEntrada && fonte != null && !fadeSaidaEmAndamento)
        {
            tempo += Time.unscaledDeltaTime;
            fonte.volume = Mathf.Lerp(0f, VolumeMusicaConfigurado(), Mathf.Clamp01(tempo / DuracaoFadeEntrada));
            yield return null;
        }
        fadeEntradaEmAndamento = false;
        if (fonte != null && !fadeSaidaEmAndamento)
            fonte.volume = VolumeMusicaConfigurado();
    }

    public static void IniciarFadeSaida(float duracao)
    {
        if (instancia == null || instancia.fonte == null || !instancia.fonte.isPlaying
            || instancia.fadeSaidaEmAndamento) return;

        instancia.StartCoroutine(instancia.FadeSaida(duracao));
    }

    private IEnumerator FadeSaida(float duracao)
    {
        fadeSaidaEmAndamento = true;
        float volumeInicial = fonte.volume;
        float tempo = 0f;
        duracao = Mathf.Max(duracao, 0f);
        while (tempo < duracao && fonte != null)
        {
            tempo += Time.unscaledDeltaTime;
            fonte.volume = Mathf.Lerp(volumeInicial, 0f, duracao > 0f ? Mathf.Clamp01(tempo / duracao) : 1f);
            yield return null;
        }
        if (fonte != null)
        {
            fonte.volume = 0f;
            fonte.Stop();
        }
        fadeSaidaEmAndamento = false;
    }

    private static float VolumeMusicaConfigurado()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolumeMusica, 1f));
    }
}
