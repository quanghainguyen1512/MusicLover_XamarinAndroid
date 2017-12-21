namespace Music_Lover.Models
{
    public class Album
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; }
        public int Id { get; set; }
        public int SongCount { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
    }
}