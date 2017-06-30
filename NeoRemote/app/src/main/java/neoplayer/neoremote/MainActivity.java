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

    private QueuedFragment queuedFragment;
    private SocketService socketService;
    private final ArrayList<MediaData> queuedVideos = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prepareSocketService();
        prepareMediaSession();
        createUIElements();
    }

    private void createUIElements() {
        queuedFragment = new QueuedFragment(this, queuedVideos);
        Fragment[] pages = new Fragment[]{
                queuedFragment
        };
        ((ViewPager) findViewById(R.id.pager)).setAdapter(new ScreenSlidePagerAdapter(getFragmentManager(), pages));
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
                socketService = ((SocketService.SocketServiceBinder) service).getService();
                socketService.setBroadcastManager(broadcastManager);
            }

            @Override
            public void onServiceDisconnected(ComponentName arg0) {
                socketService = null;
            }
        };

        bindService(new Intent(this, SocketService.class), connection, Context.BIND_AUTO_CREATE);
    }

    private void handleMessage(Intent intent) {
        Bundle extras = intent.getExtras();
        if (extras.containsKey("Queue")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("Queue");
            queuedVideos.clear();
            for (MediaData mediaData : mediaDatas)
                queuedVideos.add(mediaData);
            queuedFragment.Refresh();
        }
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
