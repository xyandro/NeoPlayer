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
import android.text.format.DateUtils;
import android.util.Log;
import android.view.View;
import android.widget.ImageButton;
import android.widget.SeekBar;
import android.widget.TextView;

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
    private boolean userTrackingSeekBar = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prepareSocketService();
        prepareMediaSession();
        createUIElements();
        hookButtons();
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

    private void hookButtons() {
        ((SeekBar) findViewById(R.id.seek_bar)).setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                ((TextView) findViewById(R.id.current_time)).setText(DateUtils.formatElapsedTime(value));
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
                userTrackingSeekBar = true;
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                socketClient.setPosition(seekBar.getProgress(), false);
                userTrackingSeekBar = false;
            }
        });

        findViewById(R.id.back30).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(-30, true);
            }
        });

        findViewById(R.id.back5).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(-5, true);
            }
        });

        findViewById(R.id.play).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.play();
            }
        });

        findViewById(R.id.forward5).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(5, true);
            }
        });

        findViewById(R.id.forward30).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(30, true);
            }
        });

        findViewById(R.id.forward).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.forward();
            }
        });
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
            ((ImageButton) findViewById(R.id.play)).setImageResource((boolean) extras.get("Playing") ? R.drawable.pause : R.drawable.play);
        }

        if (extras.containsKey("Title")) {
            ((TextView) findViewById(R.id.title)).setText((String) extras.get("Title"));
        }

        if (extras.containsKey("Position")) {
            if (!userTrackingSeekBar)
                ((SeekBar) findViewById(R.id.seek_bar)).setProgress((int) extras.get("Position"));

        }

        if (extras.containsKey("MaxPosition")) {
            int maxPosition = (int) extras.get("MaxPosition");
            ((SeekBar) findViewById(R.id.seek_bar)).setMax(maxPosition);
            ((TextView) findViewById(R.id.max_time)).setText(DateUtils.formatElapsedTime(maxPosition));
        }
    }

    public void queueVideo(MediaData mediaData) {
        socketClient.queueVideo(mediaData);
    }

    public void searchYouTube(String search) {
        socketClient.requestYouTube(search);
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
