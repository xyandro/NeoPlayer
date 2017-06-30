package neoplayer.neoremote;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class Message {
    public enum ServerCommand {
        None,
        GetQueue,
    }

    private ByteBuffer byteBuffer;
    public final ServerCommand command;

    public Message(ServerCommand command) {
        this.command = command;
        byteBuffer = ByteBuffer.allocateDirect(1024).order(ByteOrder.LITTLE_ENDIAN);
        writeInt(0);
        writeInt(command.ordinal());
    }

    public void writeInt(int value) {
        byteBuffer.putInt(value);
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

    public int readInt() {
        return byteBuffer.getInt();
    }

    public String readString() {
        int size = readInt();
        byte[] bytes = new byte[size];
        byteBuffer.get(bytes, 0, size);
        try {
            return new String(bytes, "UTF-8");
        } catch (Exception ex) {
            return null;
        }
    }
}
