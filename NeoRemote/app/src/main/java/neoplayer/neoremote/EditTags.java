package neoplayer.neoremote;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

public class EditTags {
    public ArrayList<Integer> videoFileIDs = new ArrayList<>();
    public HashMap<String, String> tags = new HashMap<>();

    public static EditTags create(ArrayList<VideoFile> videoFiles) {
        EditTags editTags = new EditTags();
        for (VideoFile videoFile : videoFiles) {
            editTags.videoFileIDs.add(videoFile.videoFileID);
            for (Map.Entry<String, String> tag : videoFile.tags.entrySet()) {
                String key = tag.getKey();
                String value = tag.getValue();
                if ((editTags.tags.containsKey(key)) && ((editTags.tags.get(key) == null) || (!editTags.tags.get(key).equals(value))))
                    value = null;
                editTags.tags.put(key, value);
            }
        }
        return editTags;
    }

    public void removeNulls() {
        Iterator<Map.Entry<String, String>> itr = tags.entrySet().iterator();
        while (itr.hasNext()) {
            Map.Entry<String, String> entry = itr.next();
            if (entry.getValue() == null)
                itr.remove();
        }
    }
}
