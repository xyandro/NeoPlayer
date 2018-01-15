package neoplayer.neoremote;

import android.app.Activity;
import android.content.Context;
import android.graphics.Color;
import android.text.InputType;
import android.util.AttributeSet;
import android.view.KeyEvent;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;
import android.widget.TextView;

public class NEEditText extends EditText {
    private Integer clickViewResource = null;

    public NEEditText(final Context context, AttributeSet attrs) {
        super(context, attrs);

        boolean hasInputType = false;
        for (int x = 0; x < attrs.getAttributeCount(); ++x) {
            String attr = attrs.getAttributeName(x);
            if (attr.equals("inputType"))
                hasInputType = true;
            else if (attr.equals("clickView")) {
                clickViewResource = attrs.getAttributeResourceValue(x, 0);
            }
        }

        if (!hasInputType)
            setInputType(InputType.TYPE_CLASS_TEXT);
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

        if (clickViewResource != null) {
            setOnEditorActionListener(new TextView.OnEditorActionListener() {
                @Override
                public boolean onEditorAction(TextView textView, int i, KeyEvent keyEvent) {
                    ((Activity) getContext()).findViewById(clickViewResource).performClick();
                    return true;
                }
            });
        }
    }
}
