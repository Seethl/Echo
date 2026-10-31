using UnityEngine;

namespace Echo.Manor
{
    public enum ManorArea { Caretaker, Gallery, Hall, Library, Kitchen, Conservatory, Yard, UpperGallery, Study }
    public sealed class ManorZone : MonoBehaviour
    {
        public ManorArea area;
        public string ukrainianName;
        public Vector3 center;
        public Vector3 size;
        public bool tutorialSafe;
        public bool Contains(Vector3 point) => new Bounds(center, size).Contains(point);
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = tutorialSafe ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
