package neoplayer.neoremote;

public class MediaData {
    public String description;
    public String url;
    public long playlistOrder;

    public MediaData(String description, String url, long playlistOrder) {
        this.description = description;
        this.url = url;
        this.playlistOrder = playlistOrder;
    }
}