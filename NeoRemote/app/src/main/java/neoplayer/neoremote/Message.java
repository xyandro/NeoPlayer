package neoplayer.neoremote;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;

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

    public Message add(MediaData mediaData) {
        add(mediaData.description);
        add(mediaData.url);
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

    public long getLong() {
        return byteBuffer.getLong();
    }

    public String getString() {
        int size = getInt();
        byte[] bytes = new byte[size];
        byteBuffer.get(bytes, 0, size);
        try {
            return new String(bytes, "UTF-8");
        } catch (Exception ex) {
            return null;
        }
    }

    public MediaData getMediaData() {
        String description = getString();
        String url = getString();
        long playlistOrder = getLong();
        return new MediaData(description, url, playlistOrder);
    }

    public ArrayList<MediaData> getMediaDatas() {
        int count = getInt();
        ArrayList<MediaData> mediaDatas = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            mediaDatas.add(getMediaData());
        }
        return mediaDatas;
    }
}
