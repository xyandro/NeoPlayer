package neoplayer.neoremote;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

public class Message {
    private ByteBuffer byteBuffer;

    public Message() {
        byteBuffer = ByteBuffer.allocateDirect(1024).order(ByteOrder.LITTLE_ENDIAN);
        add(0);
    }

    public Message add(byte[] value) {
        byteBuffer.put(value);
        return this;
    }

    public Message add(boolean value) {
        byteBuffer.put((byte) (value ? 1 : 0));
        return this;
    }

    public Message add(int value) {
        byteBuffer.putInt(value);
        return this;
    }

    public Message add(String value) {
        byte[] bytes;
        try {
            bytes = value.getBytes("UTF-8");
        } catch (Exception ex) {
            bytes = new byte[0];
        }
        add(bytes.length);
        add(bytes);
        return this;
    }

    public Message add(VideoFile videoFile) {
        add(videoFile.videoFileID);
        add(videoFile.title);
        return this;
    }

    public Message add(EditTags editTags) {
        add(editTags.videoFileIDs.size());
        for (int videoFileID : editTags.videoFileIDs)
            add(videoFileID);
        add(editTags.tags.size());
        for (Map.Entry<String, String> tag : editTags.tags.entrySet()) {
            add(tag.getKey());
            add(tag.getValue());
        }
        return this;
    }

    public byte[] toArray() {
        // Write size
        int size = byteBuffer.position();
        byteBuffer.position(0);
        byteBuffer.putInt(size);
        byteBuffer.position(size);

        byteBuffer.flip();
        byte[] arr = new byte[byteBuffer.remaining()];
        byteBuffer.get(arr);
        return arr;
    }

    public Message(InputStream stream) throws IOException {
        byte[] buffer = new byte[4];
        int size = 4;
        int used = 0;
        boolean first = true;
        while (used < size) {
            int block = stream.read(buffer, used, buffer.length - used);
            if (block == -1)
                throw new IOException();
            used += block;
            if ((first) && (used == size)) {
                first = false;
                size = ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).getInt() - 4;
                buffer = new byte[size];
                used = 0;
            }
        }
        byteBuffer = byteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN);
    }

    public boolean getBool() {
        return byteBuffer.get() != 0;
    }

    public int getInt() {
        return byteBuffer.getInt();
    }

    public ArrayList<Integer> getInts() {
        int count = getInt();
        ArrayList<Integer> ints = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            ints.add(getInt());
        }
        return ints;
    }

    public String getString() {
        int size = getInt();
        if (size == -1)
            return null;

        byte[] bytes = new byte[size];
        byteBuffer.get(bytes, 0, size);
        try {
            return new String(bytes, "UTF-8");
        } catch (Exception ex) {
            return null;
        }
    }

    public VideoFile getVideoFile() {
        int videoFileID = getInt();
        String title = getString();
        int count = getInt();
        HashMap<String, String> tagValues = new HashMap<>();
        for (int ctr = 0; ctr < count; ++ctr)
            tagValues.put(getString(), getString());
        return new VideoFile(videoFileID, title, tagValues);
    }

    public ArrayList<VideoFile> getVideoFiles() {
        int count = getInt();
        ArrayList<VideoFile> videoFiles = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            videoFiles.add(getVideoFile());
        }
        return videoFiles;
    }

    public DownloadData getDownloadData() {
        String title = getString();
        int progress = getInt();
        return new DownloadData(title, progress);
    }

    public ArrayList<DownloadData> getDownloadDatas() {
        int count = getInt();
        ArrayList<DownloadData> downloadDatas = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            downloadDatas.add(getDownloadData());
        }
        return downloadDatas;
    }
}
