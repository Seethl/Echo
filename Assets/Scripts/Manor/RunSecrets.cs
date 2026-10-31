using UnityEngine;

namespace Echo.Manor
{
    // Answers are generated once by ManorProgress and live in the same checkpoint dictionary.
    public static class RunSecrets
    {
        public const string GalleryKey="secret.gallery.code",LibraryKey="secret.library.order";
        private static readonly string[][] orders={
            new[]{"moon","tree","bird"},new[]{"moon","bird","tree"},
            new[]{"tree","moon","bird"},new[]{"tree","bird","moon"},
            new[]{"bird","moon","tree"},new[]{"bird","tree","moon"}};
        public static int NewCode(int previous)
        {int code=Random.Range(100,1000);return code==previous?100+(code-99)%900:code;}
        public static int NewOrder(int previous)
        {int order=Random.Range(1,orders.Length+1);return order==previous?order%orders.Length+1:order;}
        public static string[] Order(int index)=>orders[Mathf.Clamp(index-1,0,orders.Length-1)];
        public static string SpokenCode(string code)=>string.Join(" — ",code.ToCharArray());
    }
}
