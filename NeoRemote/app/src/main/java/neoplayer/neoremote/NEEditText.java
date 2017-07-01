package neoplayer.neoremote;

import android.content.Context;
import android.graphics.Color;
import android.text.InputType;
import android.util.AttributeSet;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;

public class NEEditText extends EditText {
    public NEEditText(final Context context, AttributeSet attrs) {
        super(context, attrs);

        setInputType(InputType.TYPE_CLASS_TEXT);
        setMinLines(1);
        setLines(1);
        setMaxLines(1);
        setSelectAllOnFocus(true);
        setTextColor(Color.BLACK);

        setOnFocusChangeListener(new OnFocusChangeListener() {
            @Override
            public void onFocusChange(View view, boolean hasFocus) {
                if (hasFocus)
                    return;
                InputMethodManager imm = (InputMethodManager) context.getSystemService(Context.INPUT_METHOD_SERVICE);
                imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
            }
        });
    }
}
