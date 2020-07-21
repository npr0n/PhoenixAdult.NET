using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers
{
    public class SceneSearch
    {
        public string CurID { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Poster { get; set; }
        public int? IndexNumber { get; set; }
    }

    public class Scene
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Studios { get; } = new List<string>();
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; } = new List<string>();
        public List<Actor> Actors { get; } = new List<Actor>();
        public List<string> Posters { get; } = new List<string>();
        public List<string> Backgrounds { get; } = new List<string>();
    }

    public class Actor
    {
        public string Name { get; set; }
        public string Photo { get; set; }
    }

    public interface IPhoenixAdultNETProviderBase
    {
        Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken);

        Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken);
    }
}
