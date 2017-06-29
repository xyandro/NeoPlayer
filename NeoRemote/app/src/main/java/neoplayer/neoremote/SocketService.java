package neoplayer.neoremote;

import android.app.Service;
import android.content.Intent;
import android.os.Binder;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class SocketService extends Service {
    private final SocketServiceBinder mBinder = new SocketServiceBinder();
    private LocalBroadcastManager mLocalBroadcastManager;

    @Override
    public IBinder onBind(Intent intent) {
        return mBinder;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
    }

    private byte[] ReadSocket(InputStream stream, int size) throws IOException {
        byte[] buffer = new byte[size];
        int used = 0;
        while (used < buffer.length) {
            int block = stream.read(buffer, used, buffer.length - used);
            if (block == -1)
                throw new IOException();
            used += block;
        }
        return buffer;
    }

    private void RunReaderThread() {
        while (true) {
            try {
                final Socket socket = new Socket("192.168.1.10", 7398);
                new Thread(new Runnable() {
                    @Override
                    public void run() {
                        RunWriterThread(socket);
                    }
                }).start();

                InputStream stream = socket.getInputStream();

                while (true) {
                    byte[] buffer = ReadSocket(stream, 4);
                    int size = ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).getInt();
                    buffer = ReadSocket(stream, size);
                    mLocalBroadcastManager.sendBroadcast(new Intent("custom-event-name"));
                }
            } catch (Exception ex) {
            }
        }
    }

    private void RunWriterThread(Socket socket) {
        try {
            OutputStream stream = socket.getOutputStream();
            byte[] message = new byte[]{51, 0, 0, 0, 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 116, 101, 115, 116, 32, 115, 116, 114, 105, 110, 103, 46, 32, 73, 116, 39, 115, 32, 112, 114, 101, 116, 116, 121, 32, 97, 119, 101, 115, 111, 109, 101, 44, 32, 114, 105, 103, 104, 116, 63};
            stream.write(message);
        } catch (Exception ex) {
        }
    }

    public class SocketServiceBinder extends Binder {
        SocketService getService() {
            return SocketService.this;
        }
    }

    public void SetBroadcastManager(LocalBroadcastManager localBroadcastManager) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                RunReaderThread();
            }
        }).start();
        mLocalBroadcastManager = localBroadcastManager;
    }
}
