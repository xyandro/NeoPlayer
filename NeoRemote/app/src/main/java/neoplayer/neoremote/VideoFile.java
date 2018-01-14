package neoplayer.neoremote;

public class VideoFile {
    public int videoFileID;
    public String title;
    public int playlistOrder;

    public VideoFile(int videoFileID, String title, int playlistOrder) {
        this.videoFileID = videoFileID;
        this.title = title;
        this.playlistOrder = playlistOrder;
    }
}