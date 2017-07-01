package neoplayer.neoremote;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.IBinder;
import android.support.v13.app.FragmentStatePagerAdapter;
import android.support.v4.content.LocalBroadcastManager;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.support.v4.view.ViewPager;
import android.util.Log;

import java.util.ArrayList;

public class MainActivity extends Activity {
    private static final String TAG = MainActivity.class.getSimpleName();

    private QueueFragment queueFragment;
    private CoolFragment coolFragment;
    private YouTubeFragment youTubeFragment;
    private SocketClient socketClient;
    private final ArrayList<MediaData> queueVideos = new ArrayList<>();
    private final ArrayList<MediaData> coolVideos = new ArrayList<>();
    private final ArrayList<MediaData> youTubeVideos = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prepareSocketService();
        prepareMediaSession();
        createUIElements();
    }

    private void createUIElements() {
        queueFragment = new QueueFragment(this, queueVideos);
        coolFragment = new CoolFragment(this, coolVideos, queueVideos);
        youTubeFragment = new YouTubeFragment(this, youTubeVideos, queueVideos);
        Fragment[] pages = new Fragment[]{
                queueFragment,
                coolFragment,
                youTubeFragment,
        };
        ViewPager pager = findViewById(R.id.pager);
        pager.setAdapter(new ScreenSlidePagerAdapter(getFragmentManager(), pages));
        pager.setCurrentItem(1);
    }

    private void prepareMediaSession() {
        MediaSessionCompat mediaSession = new MediaSessionCompat(this, "NeoRemoteMediaSession");
        mediaSession.setPlaybackState(new PlaybackStateCompat.Builder()
                .setState(PlaybackStateCompat.STATE_PLAYING, PlaybackStateCompat.PLAYBACK_POSITION_UNKNOWN, 1.0f)
                .build());
        mediaSession.setCallback(new MediaSessionCompat.Callback() {
            @Override
            public boolean onMediaButtonEvent(final Intent mediaButtonEvent) {
                Log.d(TAG, "onMediaButtonEvent() called with: " + "mediaButtonEvent = [" + mediaButtonEvent + "]");
                return super.onMediaButtonEvent(mediaButtonEvent);
            }
        });

        VolumeProviderCompat volumeProvider = new VolumeProviderCompat(VolumeProviderCompat.VOLUME_CONTROL_ABSOLUTE, 100, 50) {
            @Override
            public void onSetVolumeTo(int volume) {
                setCurrentVolume(volume);
                Log.d(TAG, "onSetVolumeTo: " + volume);
            }

            @Override
            public void onAdjustVolume(int delta) {
                int newVolume = getCurrentVolume() + delta;
                setCurrentVolume(newVolume);
                Log.d(TAG, "onAdjustVolume: " + newVolume);
            }
        };

        mediaSession.setPlaybackToRemote(volumeProvider);
        mediaSession.setActive(true);
    }

    private void prepareSocketService() {
        BroadcastReceiver broadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                handleMessage(intent);
            }
        };
        final LocalBroadcastManager broadcastManager = LocalBroadcastManager.getInstance(this);
        broadcastManager.registerReceiver(broadcastReceiver, new IntentFilter("NeoRemoteEvent"));

        ServiceConnection connection = new ServiceConnection() {
            @Override
            public void onServiceConnected(ComponentName className, IBinder service) {
                socketClient = ((SocketClient.SocketServiceBinder) service).getService();
                socketClient.setBroadcastManager(broadcastManager);
            }

            @Override
            public void onServiceDisconnected(ComponentName arg0) {
                socketClient = null;
            }
        };

        bindService(new Intent(this, SocketClient.class), connection, Context.BIND_AUTO_CREATE);
    }

    private void handleMessage(Intent intent) {
        Bundle extras = intent.getExtras();

        if (extras.containsKey("Queue")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("Queue");
            queueVideos.clear();
            for (MediaData mediaData : mediaDatas)
                queueVideos.add(mediaData);
            queueFragment.Refresh();
            coolFragment.Refresh();
            youTubeFragment.Refresh();
        }

        if (extras.containsKey("Cool")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("Cool");
            coolVideos.clear();
            for (MediaData mediaData : mediaDatas)
                coolVideos.add(mediaData);
            coolFragment.Refresh();
        }

        if (extras.containsKey("YouTube")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("YouTube");
            youTubeVideos.clear();
            for (MediaData mediaData : mediaDatas)
                youTubeVideos.add(mediaData);
            youTubeFragment.Refresh();
        }

        if (extras.containsKey("Playing")) {
            coolFragment.setPlaying((boolean) extras.get("Playing"));
        }

        if (extras.containsKey("Title")) {
            coolFragment.setTitle((String) extras.get("Title"));
        }

        if (extras.containsKey("Position")) {
            coolFragment.setPosition((int) extras.get("Position"));
        }

        if (extras.containsKey("MaxPosition")) {
            coolFragment.setMaxPosition((int) extras.get("MaxPosition"));
        }
    }

    public void queueVideo(MediaData mediaData) {
        socketClient.queueVideo(mediaData);
    }

    public void searchYouTube(String search) {
        socketClient.requestYouTube(search);
    }

    public void setPosition(int offset, boolean relative) {
        socketClient.setPosition(offset, relative);
    }

    public void play() {
        socketClient.play();
    }

    public void forward() {
        socketClient.forward();
    }

    private class ScreenSlidePagerAdapter extends FragmentStatePagerAdapter {
        private Fragment[] pages;

        public ScreenSlidePagerAdapter(FragmentManager fm, Fragment[] pages) {
            super(fm);
            this.pages = pages;
        }

        @Override
        public Fragment getItem(int position) {
            return pages[position];
        }

        @Override
        public int getCount() {
            return pages.length;
        }
    }
}
