package neoplayer.neoremote;

import android.app.Fragment;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.text.format.DateUtils;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.SeekBar;
import android.widget.Spinner;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.LinkedHashMap;

public class SlidesFragment extends Fragment {
    private static final String TAG = SlidesFragment.class.getSimpleName();

    private static final LinkedHashMap<String, String> validSizes = new LinkedHashMap<String, String>();

    static {
        validSizes.put("Any size", "");
        validSizes.put("Large", "l");
        validSizes.put("Medium", "m");
        validSizes.put("Icon", "i");
        validSizes.put("400x300", "qsvga");
        validSizes.put("640x480", "vga");
        validSizes.put("800x600", "svga");
        validSizes.put("1024x768", "xga");
        validSizes.put("2 MP", "2mp");
        validSizes.put("4 MP", "4mp");
        validSizes.put("6 MP", "6mp");
        validSizes.put("8 MP", "8mp");
        validSizes.put("10 MP", "10mp");
        validSizes.put("12 MP", "12mp");
        validSizes.put("15 MP", "15mp");
        validSizes.put("20 MP", "20mp");
        validSizes.put("40 MP", "40mp");
        validSizes.put("70 MP", "70mp");
    }

    private final MainActivity mainActivity;

    public SlidesFragment(MainActivity mainActivity) {
        this.mainActivity = mainActivity;
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        final View result = inflater.inflate(R.layout.fragment_slides, container, false);

        ((NEEditText) result.findViewById(R.id.slides_query)).addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                if (charSequence.toString().endsWith("\n\n"))
                    result.findViewById(R.id.slides_submit).performClick();
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });
        final Spinner spinner = result.findViewById(R.id.slides_size);
        ArrayAdapter<String> adapter = new ArrayAdapter<String>(mainActivity, R.layout.spinner_item, new ArrayList<String>(validSizes.keySet()));
        spinner.setAdapter(adapter);

        result.findViewById(R.id.slides_submit).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                result.findViewById(R.id.slides_query).clearFocus();
                String query = ((EditText) result.findViewById(R.id.slides_query)).getText().toString();
                String size = validSizes.get(spinner.getSelectedItem());
                mainActivity.setSlidesData(query, size);
            }
        });

        ((SeekBar) result.findViewById(R.id.slides_display_time)).setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                int displayTime = seekBarToDisplayTime(value);
                value = displayTimeToSeekBar(displayTime);
                seekBar.setProgress(value);
                ((TextView) result.findViewById(R.id.slides_display_time_text)).setText(DateUtils.formatElapsedTime(displayTime));
                if (fromUser)
                    mainActivity.setSlideDisplayTime(displayTime);
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
            }
        });

        result.findViewById(R.id.slides_back).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.cycleSlide(false);
            }
        });

        result.findViewById(R.id.slides_play).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.pauseSlides();
            }
        });

        result.findViewById(R.id.slides_forward).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.cycleSlide(true);
            }
        });

        return result;
    }

    private int seekBarToDisplayTime(int value) {
        if (value <= 0)
            return 2;
        if (value <= 6)
            return value * 5;
        if (value <= 8)
            return value * 15 - 60;
        if (value <= 12)
            return value * 30 - 180;
        return value * 60 - 540;
    }

    private int displayTimeToSeekBar(int value) {
        if (value <= 3)
            return 0;
        if (value <= 30)
            return (value + 2) / 5;
        if (value <= 60)
            return (value + 7) / 15 + 4;
        if (value <= 180)
            return (value + 14) / 30 + 6;
        return (value + 29) / 60 + 9;
    }

    public void setSlidesQuery(String slidesQuery) {
        ((EditText) getView().findViewById(R.id.slides_query)).setText(slidesQuery);
    }

    public void setSlidesSize(String slidesSize) {
        int index = 0;
        for (String entry : validSizes.keySet()) {
            if (validSizes.get(entry).equals(slidesSize))
                ((Spinner) getView().findViewById(R.id.slides_size)).setSelection(index);
            ++index;
        }
    }

    public void setSlideDisplayTime(int slideDisplayTime) {
        ((SeekBar) getView().findViewById(R.id.slides_display_time)).setProgress(displayTimeToSeekBar(slideDisplayTime));
    }

    public void setSlidesPaused(boolean slidesPaused) {
        ((ImageButton) getView().findViewById(R.id.slides_play)).setImageResource(slidesPaused ? R.drawable.play : R.drawable.pause);
    }
}
