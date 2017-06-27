package neoplayer.neoremote;

import android.content.Intent;
import android.os.Bundle;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;

public class MainActivity extends AppCompatActivity {

    private static final String TAG = MainActivity.class.getSimpleName();

    private MediaSessionCompat session;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        session = new MediaSessionCompat(this, "demoMediaSession");
        session.setPlaybackState(new PlaybackStateCompat.Builder()
                .setState(PlaybackStateCompat.STATE_PLAYING, PlaybackStateCompat.PLAYBACK_POSITION_UNKNOWN, 1.0f)
                .build());
        session.setCallback(new MediaSessionCompat.Callback() {
            @Override
            public boolean onMediaButtonEvent(final Intent mediaButtonEvent) {
                Log.d(TAG, "onMediaButtonEvent() called with: " + "mediaButtonEvent = [" + mediaButtonEvent + "]");
                return super.onMediaButtonEvent(mediaButtonEvent);
            }
        });
        session.setPlaybackToRemote(createVolumeProvider());
        session.setActive(true);
    }

    private VolumeProviderCompat createVolumeProvider() {
        return new VolumeProviderCompat(VolumeProviderCompat.VOLUME_CONTROL_ABSOLUTE,
                100,
                50) {

            @Override
            public void onSetVolumeTo(int volume) {
                setCurrentVolume(volume);
                Log.d(TAG, "onSetVolumeTo: " + volume);
            }

            @Override
            public void onAdjustVolume(int delta) {
                int newVolume = getCurrentVolume() + delta;
                setCurrentVolume(newVolume);
                Log.d(TAG, "onSetVolumeTo: " + newVolume);
            }
        };
    }
}
