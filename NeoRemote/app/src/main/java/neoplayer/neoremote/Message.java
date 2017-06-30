package neoplayer.neoremote;

import java.io.IOException;
import java.io.InputStream;
import java.io.UnsupportedEncodingException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class Message {
    public enum ServerCommand {
        None,
        Queued,
    }

    ByteBuffer byteBuffer;
    public final ServerCommand command;

    public Message(ServerCommand command) {
        this.command = command;
        byteBuffer = ByteBuffer.allocateDirect(1024).order(ByteOrder.LITTLE_ENDIAN);
        WriteInt(8);
        WriteInt(command.ordinal());
    }

    public void WriteInt(int value) {
        byteBuffer.putInt(value);
    }

    public byte[] GetBytes() {
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

    public int ReadInt() {
        return byteBuffer.getInt();
    }

    public String ReadString() throws UnsupportedEncodingException {
        int size = ReadInt();
        byte[] bytes = new byte[size];
        byteBuffer.get(bytes, 0, size);
        return new String(bytes, "UTF-8");
    }
}