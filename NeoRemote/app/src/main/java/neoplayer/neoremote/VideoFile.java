package neoplayer.neoremote;

import java.util.HashMap;

public class VideoFile {
    public int videoFileID;
    public String title;
    public HashMap<String, String> tagValues = new HashMap<>();

    public VideoFile(int videoFileID, String title, HashMap<String, String> tagValues ) {
        this.videoFileID = videoFileID;
        this.title = title;
        this.tagValues = tagValues;
    }
}