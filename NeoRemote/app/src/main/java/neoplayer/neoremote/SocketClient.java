package neoplayer.neoremote;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Binder;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;
import android.util.Log;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.InterfaceAddress;
import java.net.NetworkInterface;
import java.net.Socket;
import java.net.UnknownHostException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.StringTokenizer;
import java.util.concurrent.ArrayBlockingQueue;

public class SocketClient extends Service {
    private static final String TAG = SocketClient.class.getSimpleName();
    private static final int NeoPlayerToken = 0xfeedbeef;
    private static final int NeoPlayerRestartToken = 0x0badf00d;
    private static final int NeoPlayerPort = 7399;
    private static final int NeoPlayerRestartPort = 7398;
    private static final String addressFileName = "NeoPlayer.txt";

    private final SocketServiceBinder binder = new SocketServiceBinder();
    private LocalBroadcastManager broadcastManager;
    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<>(100);
    private InetAddress neoPlayerAddress = null;

    public void setAddress(final String address) {
        try {
            neoPlayerAddress = new AsyncTask<Void, Void, InetAddress>() {
                @Override
                protected InetAddress doInBackground(Void... voids) {
                    try {
                        return InetAddress.getByName(address);
                    } catch (Exception ex) {
                        return null;
                    }
                }
            }.execute().get();
            if (neoPlayerAddress == null)
                return;

            byte[] buffer = address.getBytes("UTF-8");
            byte[] bufferSize = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(buffer.length).array();
            OutputStream out = openFileOutput(addressFileName, Context.MODE_PRIVATE);
            out.write(bufferSize);
            out.write(buffer);
            out.close();
        } catch (Exception ex) {
            Intent intent = new Intent("NeoRemoteEvent");
            intent.putExtra("Toast", "Error: " + ex.getMessage());
            broadcastManager.sendBroadcast(intent);
        }
    }

    @Override
    public IBinder onBind(Intent intent) {
        return binder;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
    }

    public static String findNeoPlayer() {
        try {
            return new AsyncTask<Void, Void, String>() {
                @Override
                protected String doInBackground(Void... voids) {
                    try {
                        Log.d(TAG, "findNeoPlayer: Scanning for NeoPlayer...");
                        DatagramSocket socket = new DatagramSocket();
                        socket.setBroadcast(true);
                        socket.setSoTimeout(1000);
                        byte[] message = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(NeoPlayerToken).array();

                        for (Enumeration<NetworkInterface> niEnum = NetworkInterface.getNetworkInterfaces(); niEnum.hasMoreElements(); ) {
                            NetworkInterface ni = niEnum.nextElement();
                            if (ni.isLoopback())
                                continue;

                            for (InterfaceAddress interfaceAddress : ni.getInterfaceAddresses()) {
                                if (interfaceAddress.getBroadcast() == null)
                                    continue;

                                DatagramPacket packet = new DatagramPacket(message, message.length, interfaceAddress.getBroadcast(), NeoPlayerPort);
                                socket.send(packet);
                            }
                        }

                        DatagramPacket packet = new DatagramPacket(message, message.length);
                        while (true) {
                            socket.receive(packet);

                            if (packet.getData().length != 4)
                                continue;

                            if (ByteBuffer.wrap(packet.getData()).order(ByteOrder.LITTLE_ENDIAN).getInt() != NeoPlayerToken)
                                continue;

                            return packet.getAddress().getHostAddress();
                        }
                    } catch (Exception ex) {
                        return null;
                    }
                }
            }.execute().get();
        } catch (Exception ex) {
            return null;
        }
    }

    private void getNeoPlayerAddress() {
        try {
            InputStream in = openFileInput(addressFileName);
            try {
                byte[] buffer = new byte[4];
                in.read(buffer, 0, 4);
                int size = ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).getInt();
                buffer = new byte[size];
                in.read(buffer, 0, size);
                String address = new String(buffer, "UTF-8");
                neoPlayerAddress = InetAddress.getByName(address);
            } finally {
                in.close();
            }
        } catch (Exception ex) {
        }
    }

    private void runReaderThread() {
        getNeoPlayerAddress();

        Log.d(TAG, "runReaderThread: Started");
        while (true) try {
//            findNeoPlayer();

            final Socket socket = new Socket();
            socket.connect(new InetSocketAddress(neoPlayerAddress, NeoPlayerPort), 1000);

            try {
                Log.d(TAG, "runReaderThread: Connected");

                Intent intent = new Intent("NeoRemoteEvent");
                intent.putExtra("GotNeoPlayer", neoPlayerAddress);
                broadcastManager.sendBroadcast(intent);

                outputQueue.clear();
                requestQueue();
                requestCool();
                requestMediaData();
                requestSlidesData();
                requestVolume();

                new Thread(new Runnable() {
                    @Override
                    public void run() {
                        runWriterThread(socket);
                    }
                }).start();

                while (true) {
                    Message message = new Message(socket.getInputStream());
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
                        case GetMediaData:
                            setMediaData(message);
                            break;
                        case GetVolume:
                            setVolume(message);
                            break;
                        case GetSlidesData:
                            setSlidesData(message);
                            break;
                    }
                }
            } finally {
                socket.close();
            }
        } catch (Exception ex) {
            Log.d(TAG, "runReaderThread: Error: " + ex.getMessage());
            outputQueue.clear();
            outputQueue.add(new byte[0]); // Let writer thread know there's a problem

            Intent intent = new Intent("NeoRemoteEvent");
            intent.putExtra("FindNeoPlayer", neoPlayerAddress == null ? null : neoPlayerAddress.getHostAddress());
            broadcastManager.sendBroadcast(intent);

            try {
                Thread.sleep(1000);
            } catch (Exception e) {
            }
        }
    }

    private void requestQueue() {
        Log.d(TAG, "requestQueue: Requesting current queue");
        outputQueue.add(new Message(Message.ServerCommand.GetQueue).getBytes());
    }

    private void setQueue(Message message) {
        int count = message.getInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.getString();
            String url = message.getString();
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
        int count = message.getInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.getString();
            String url = message.getString();
            mediaData.add(new MediaData(description, url));
        }
        Log.d(TAG, "setCool: " + mediaData.size() + " item(s)");

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Cool", mediaData);
        broadcastManager.sendBroadcast(intent);
    }

    private void requestMediaData() {
        Log.d(TAG, "requestMediaData: Requesting media data");
        outputQueue.add(new Message(Message.ServerCommand.GetMediaData).getBytes());
    }

    private void setMediaData(Message message) {
        boolean playing = message.getBool();
        String title = message.getString();
        int position = message.getInt();
        int maxPosition = message.getInt();

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Playing", playing);
        intent.putExtra("Title", title);
        intent.putExtra("Position", position);
        intent.putExtra("MaxPosition", maxPosition);
        broadcastManager.sendBroadcast(intent);
    }

    private void requestSlidesData() {
        Log.d(TAG, "requestSlidesData: Requesting slides data");
        outputQueue.add(new Message(Message.ServerCommand.GetSlidesData).getBytes());
    }

    private void setSlidesData(Message message) {
        String slidesQuery = message.getString();
        String slidesSize = message.getString();
        int slideDisplayTime = message.getInt();
        boolean slidesPaused = message.getBool();

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("SlidesQuery", slidesQuery);
        intent.putExtra("SlidesSize", slidesSize);
        intent.putExtra("SlideDisplayTime", slideDisplayTime);
        intent.putExtra("SlidesPaused", slidesPaused);
        broadcastManager.sendBroadcast(intent);
    }

    public void setSlidesData(String query, String size) {
        Log.d(TAG, "setSlidesData: query = " + query + ", size = " + size);
        Message message = new Message(Message.ServerCommand.SetSlidesData);
        message.add(query);
        message.add(size);
        outputQueue.add(message.getBytes());
    }

    public void requestYouTube(String search) {
        Log.d(TAG, "RequestYouTube: Requesting YouTube " + search);
        Message message = new Message(Message.ServerCommand.GetYouTube);
        message.add(search);
        outputQueue.add(message.getBytes());
    }

    private void setYouTube(Message message) {
        int count = message.getInt();
        ArrayList<MediaData> mediaData = new ArrayList<>();
        for (int ctr = 0; ctr < count; ++ctr) {
            String description = message.getString();
            String url = message.getString();
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
                byte[] message = outputQueue.take();
                if (message.length == 0)
                    continue;

                socket.getOutputStream().write(message);
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

    public void setPosition(int offset, boolean relative) {
        Log.d(TAG, "setPosition: offset = " + offset);
        Message message = new Message(Message.ServerCommand.SetPosition);
        message.add(offset);
        message.add(relative);
        outputQueue.add(message.getBytes());
    }

    public void play() {
        Log.d(TAG, "play");
        Message message = new Message(Message.ServerCommand.Play);
        outputQueue.add(message.getBytes());
    }

    public void forward() {
        Log.d(TAG, "forward");
        Message message = new Message(Message.ServerCommand.Forward);
        outputQueue.add(message.getBytes());
    }

    public void requestVolume() {
        Log.d(TAG, "requestVolume");
        Message message = new Message(Message.ServerCommand.GetVolume);
        outputQueue.add(message.getBytes());
    }

    public void setVolume(Message message) {
        int volume = message.getInt();
        Log.d(TAG, "setVolume: " + volume);

        Intent intent = new Intent("NeoRemoteEvent");
        intent.putExtra("Volume", volume);
        broadcastManager.sendBroadcast(intent);
    }

    public void setVolume(int volume, boolean relative) {
        Log.d(TAG, "setVolume: " + volume);
        Message message = new Message(Message.ServerCommand.SetVolume);
        message.add(volume);
        message.add(relative);
        outputQueue.add(message.getBytes());
    }

    public void setSlideDisplayTime(int time) {
        Log.d(TAG, "setSlideDisplayTime: " + time);
        Message message = new Message(Message.ServerCommand.SetSlideDisplayTime);
        message.add(time);
        outputQueue.add(message.getBytes());
    }

    public void cycleSlide(boolean forward) {
        Log.d(TAG, "cycleSlide: " + forward);
        Message message = new Message(Message.ServerCommand.CycleSlide);
        message.add(forward);
        outputQueue.add(message.getBytes());
    }

    public void pauseSlides() {
        Log.d(TAG, "pauseSlides");
        Message message = new Message(Message.ServerCommand.PauseSlides);
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

    public void sendRestart() {
        new AsyncTask<Void, Void, Void>() {
            @Override
            protected Void doInBackground(Void... voids) {
                try {
                    if (neoPlayerAddress != null)
                        new Socket(neoPlayerAddress, NeoPlayerRestartPort).getOutputStream().write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(NeoPlayerRestartToken).array());
                } catch (Exception e) {
                }
                return null;
            }
        }.execute();
    }
}
