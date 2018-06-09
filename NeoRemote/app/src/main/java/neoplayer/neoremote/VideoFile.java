package neoplayer.neoremote;

import java.util.HashMap;

public class VideoFile {
    public int videoFileID;
    public HashMap<String, String> tags = new HashMap<>();

    public VideoFile(int videoFileID, HashMap<String, String> tags) {
        this.videoFileID = videoFileID;
        this.tags = tags;
    }

    public String getTitle() {
        return tags.get("Title");
    }

    public Boolean audioOnly() {
        return Boolean.parseBoolean(tags.get("AudioOnly"));
    }
}