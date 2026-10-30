using UnityEngine;

/// <summary>Carrega os efeitos da pasta Resources/Sons/AudioClip.</summary>
public static class SomJogo
{
    public static AudioClip Carregar(string nome) => Resources.Load<AudioClip>("Sons/AudioClip/" + nome);

    public static AudioSource CriarFonte(Transform alvo, bool espacial = true)
    {
        AudioSource fonte = alvo.GetComponent<AudioSource>();
        if (fonte == null) fonte = alvo.gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = espacial ? 1f : 0f;
        fonte.rolloffMode = AudioRolloffMode.Linear;
        fonte.minDistance = 1f;
        fonte.maxDistance = 12f;
        return fonte;
    }

    public static void Tocar(Transform alvo, string nome, float volume = 1f)
    {
        AudioClip clip = Carregar(nome);
        if (clip != null) CriarFonte(alvo).PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
