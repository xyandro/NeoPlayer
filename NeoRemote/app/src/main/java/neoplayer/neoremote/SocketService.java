package neoplayer.neoremote;

import android.app.Service;
import android.content.Intent;
import android.os.Binder;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.net.Socket;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.concurrent.ArrayBlockingQueue;

public class SocketService extends Service {
    private final SocketServiceBinder mBinder = new SocketServiceBinder();
    private LocalBroadcastManager mLocalBroadcastManager;
    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<byte[]>(100);

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
                final Socket socket = new Socket("192.168.1.10", 7399);

                RequestQueued();

                new Thread(new Runnable() {
                    @Override
                    public void run() {
                        RunWriterThread(socket);
                    }
                }).start();

                InputStream stream = socket.getInputStream();

                while (true) {
                    Message message = new Message(stream);
                    switch (message.command) {
                        case Queued:
                            SetQueued(message);
                            break;
                    }
                }
            } catch (Exception ex) {
            }
        }
    }

    private void SetQueued(Message message) throws UnsupportedEncodingException {
        int count = message.ReadInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.ReadString();
            String url = message.ReadString();
            mediaData.add(new MediaData(description, url));
        }
        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Queue", mediaData);
        mLocalBroadcastManager.sendBroadcast(intent);
    }

    private void RunWriterThread(Socket socket) {
        try {
            OutputStream stream = socket.getOutputStream();
            while (true) {
                byte[] message = outputQueue.take();
                stream.write(message);
            }
        } catch (Exception ex) {
        }
    }

    private void RequestQueued() {
        Message message = new Message(Message.ServerCommand.Queued);
        outputQueue.add(message.GetBytes());
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
