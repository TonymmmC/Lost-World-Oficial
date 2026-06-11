// Claves de audio centralizadas: el nombre del archivo (sin extension) dentro de
// Resources/Audio. Evita strings sueltos repartidos por los scripts, como pide el
// proyecto. El AudioManager las resuelve a AudioClip via Resources.Load.
public static class Sonidos
{
    public static class Musica
    {
        public const string Menu = "main menu";
        public const string Historia = "historia";
        public const string GameOver = "song gameover";
    }

    public static class Sfx
    {
        public const string Espada = "sword";
        public const string EspadaFuerte = "sword2";
        public const string Curar = "heal";
        public const string Magia = "magic attack";
        public const string PasoPasto = "walk grass";
        public const string PasoTierra = "walk dirt";
    }
}
