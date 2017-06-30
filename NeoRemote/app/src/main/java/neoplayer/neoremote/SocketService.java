package neoplayer.neoremote;

import android.app.Service;
import android.content.Intent;
import android.os.Binder;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;
import android.util.Log;

import java.io.UnsupportedEncodingException;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.util.ArrayList;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.TimeUnit;

public class SocketService extends Service {
    private final String TAG = SocketService.class.getSimpleName();

    private final SocketServiceBinder mBinder = new SocketServiceBinder();
    private LocalBroadcastManager mBroadcastManager;
    private ArrayBlockingQueue<byte[]> mOutputQueue = new ArrayBlockingQueue<>(100);

    @Override
    public IBinder onBind(Intent intent) {
        return mBinder;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
    }

    private void RunReaderThread() {
        Log.d(TAG, "RunReaderThread: Started");
        while (true) {
            try {
                Log.d(TAG, "RunReaderThread: Connecting...");

                final Socket socket = new Socket();
                socket.connect(new InetSocketAddress("192.168.1.10", 7399), 1000);

                Log.d(TAG, "RunReaderThread: Connected");

                mOutputQueue.clear();
                RequestQueue();

                new Thread(new Runnable() {
                    @Override
                    public void run() {
                        RunWriterThread(socket);
                    }
                }).start();

                while (true) {
                    Message message = new Message(socket.getInputStream());
                    Log.d(TAG, "RunReaderThread: Got message (" + message.command + ")");
                    switch (message.command) {
                        case GetQueue:
                            SetQueue(message);
                            break;
                    }
                }
            } catch (Exception ex) {
                Log.d(TAG, "RunReaderThread: Error: " + ex.getMessage());
                mOutputQueue.clear();
                try {
                    Thread.sleep(1000);
                } catch (Exception e) {
                }
            }
        }
    }

    private void RequestQueue() {
        Log.d(TAG, "RequestQueued: Requesting current queue");
        mOutputQueue.add(new Message(Message.ServerCommand.GetQueue).GetBytes());
    }

    private void SetQueue(Message message) {
        int count = message.ReadInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.ReadString();
            String url = message.ReadString();
            mediaData.add(new MediaData(description, url));
        }
        Log.d(TAG, "SetQueue: " + mediaData.size() + " item(s)");

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Queue", mediaData);
        mBroadcastManager.sendBroadcast(intent);
    }

    private void RunWriterThread(Socket socket) {
        try {
            Log.d(TAG, "RunWriterThread: Started");
            while (socket.isConnected()) {
                byte[] message = mOutputQueue.poll(1, TimeUnit.SECONDS);
                if (message == null)
                    continue;

                Log.d(TAG, "RunWriterThread: Sending message...");
                socket.getOutputStream().write(message);
                Log.d(TAG, "RunWriterThread: Done");
            }
            Log.d(TAG, "RunWriterThread: Socket disconnected");
        } catch (Exception ex) {
            Log.d(TAG, "RunWriterThread: Error: " + ex.getMessage());
        }
        Log.d(TAG, "RunWriterThread: Stopped");
    }

    public class SocketServiceBinder extends Binder {
        SocketService getService() {
            return SocketService.this;
        }
    }

    public void SetBroadcastManager(LocalBroadcastManager broadcastManager) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                RunReaderThread();
            }
        }).start();
        mBroadcastManager = broadcastManager;
    }
}
