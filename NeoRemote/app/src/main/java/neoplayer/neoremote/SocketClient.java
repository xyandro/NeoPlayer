package neoplayer.neoremote;

import android.app.Service;
import android.content.Intent;
import android.os.Binder;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;
import android.util.Log;

import java.net.InetSocketAddress;
import java.net.Socket;
import java.util.ArrayList;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.TimeUnit;

public class SocketClient extends Service {
    private static final String TAG = SocketClient.class.getSimpleName();

    private final SocketServiceBinder binder = new SocketServiceBinder();
    private LocalBroadcastManager broadcastManager;
    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<>(100);

    @Override
    public IBinder onBind(Intent intent) {
        return binder;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
    }

    private void runReaderThread() {
        Log.d(TAG, "runReaderThread: Started");
        while (true) {
            try {
                Log.d(TAG, "runReaderThread: Connecting...");

                final Socket socket = new Socket();
                socket.connect(new InetSocketAddress("192.168.1.10", 7399), 1000);

                try {
                    Log.d(TAG, "runReaderThread: Connected");

                    outputQueue.clear();
                    requestQueue();
                    requestCool();

                    new Thread(new Runnable() {
                        @Override
                        public void run() {
                            runWriterThread(socket);
                        }
                    }).start();

                    while (true) {
                        Message message = new Message(socket.getInputStream());
                        Log.d(TAG, "runReaderThread: Got message (" + message.command + ")");
                        switch (message.command) {
                            case GetQueue:
                                setQueue(message);
                                break;
                            case GetCool:
                                setCool(message);
                                break;
                            case GetYouTube:
                                setYouTube(message);
                                break;
                        }
                    }
                } catch (Exception ex) {
                    socket.close();
                    throw ex;
                }
            } catch (Exception ex) {
                Log.d(TAG, "runReaderThread: Error: " + ex.getMessage());
                outputQueue.clear();
                try {
                    Thread.sleep(1000);
                } catch (Exception e) {
                }
            }
        }
    }

    private void requestQueue() {
        Log.d(TAG, "requestQueue: Requesting current queue");
        outputQueue.add(new Message(Message.ServerCommand.GetQueue).getBytes());
    }

    private void setQueue(Message message) {
        int count = message.readInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.readString();
            String url = message.readString();
            mediaData.add(new MediaData(description, url));
        }
        Log.d(TAG, "setQueue: " + mediaData.size() + " item(s)");

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Queue", mediaData);
        broadcastManager.sendBroadcast(intent);
    }

    private void requestCool() {
        Log.d(TAG, "RequestCool: Requesting cool");
        outputQueue.add(new Message(Message.ServerCommand.GetCool).getBytes());
    }

    private void setCool(Message message) {
        int count = message.readInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.readString();
            String url = message.readString();
            mediaData.add(new MediaData(description, url));
        }
        Log.d(TAG, "setCool: " + mediaData.size() + " item(s)");

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Cool", mediaData);
        broadcastManager.sendBroadcast(intent);
    }

    public void requestYouTube(String search) {
        Log.d(TAG, "RequestYouTube: Requesting YouTube " + search);
        Message message = new Message(Message.ServerCommand.GetYouTube);
        message.add(search);
        outputQueue.add(message.getBytes());
    }

    private void setYouTube(Message message) {
        int count = message.readInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.readString();
            String url = message.readString();
            mediaData.add(new MediaData(description, url));
        }
        Log.d(TAG, "setYouTube: " + mediaData.size() + " item(s)");

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("YouTube", mediaData);
        broadcastManager.sendBroadcast(intent);
    }

    private void runWriterThread(Socket socket) {
        try {
            Log.d(TAG, "runWriterThread: Started");
            while (!socket.isClosed()) {
                byte[] message = outputQueue.poll(1, TimeUnit.SECONDS);
                if (message == null)
                    continue;

                Log.d(TAG, "runWriterThread: Sending message...");
                socket.getOutputStream().write(message);
                Log.d(TAG, "runWriterThread: Done");
            }
            Log.d(TAG, "runWriterThread: Socket disconnected");
        } catch (Exception ex) {
            Log.d(TAG, "runWriterThread: Error: " + ex.getMessage());
        }
        Log.d(TAG, "runWriterThread: Stopped");
    }

    public void queueVideo(MediaData mediaData) {
        Log.d(TAG, "queueVideo: Requesting " + mediaData.description + " (" + mediaData.url + ")");
        Message message = new Message(Message.ServerCommand.QueueVideo);
        message.add(mediaData.description);
        message.add(mediaData.url);
        outputQueue.add(message.getBytes());
    }

    public class SocketServiceBinder extends Binder {
        SocketClient getService() {
            return SocketClient.this;
        }
    }

    public void setBroadcastManager(LocalBroadcastManager broadcastManager) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                runReaderThread();
            }
        }).start();
        this.broadcastManager = broadcastManager;
    }
}
