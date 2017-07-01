package neoplayer.neoremote;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class Message {
    public enum ServerCommand {
        None,
        QueueVideo,
        GetQueue,
        GetCool,
        GetYouTube,
        SetPosition,
        Play,
        Forward,
        GetMediaData,
        GetVolume,
        SetVolume,
        GetSlidesData,
        PauseSlides,
        SetSlidesData,
        SetSlideDisplayTime,
        CycleSlide,
    }

    private ByteBuffer byteBuffer;
    public final ServerCommand command;

    public Message(ServerCommand command) {
        this.command = command;
        byteBuffer = ByteBuffer.allocateDirect(1024).order(ByteOrder.LITTLE_ENDIAN);
        add(0);
        add(command.ordinal());
    }

    public void add(byte[] value) {
        byteBuffer.put(value);
    }

    public void add(boolean value) {
        byteBuffer.put((byte) (value ? 1 : 0));
    }

    public void add(int value) {
        byteBuffer.putInt(value);
    }

    public void add(String value) {
        byte[] bytes;
        try {
            bytes = value.getBytes("UTF-8");
        } catch (Exception ex) {
            bytes = new byte[0];
        }
        add(bytes.length);
        add(bytes);
    }

    public byte[] getBytes() {
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
            if ((first) && (used == size) && (size == 4)) {
                first = false;
                size = ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).getInt() - 4;
                buffer = new byte[size];
                used = 0;
            }
        }
        byteBuffer = byteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN);
        command = ServerCommand.values()[byteBuffer.getInt()];
    }

    public boolean getBool() {
        return byteBuffer.get() != 0;
    }

    public int getInt() {
        return byteBuffer.getInt();
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
}
