package neoplayer.neoremote;

import android.app.AlertDialog;
import android.app.Dialog;
import android.app.DialogFragment;
import android.content.Context;
import android.content.DialogInterface;
import android.os.Bundle;
import android.view.View;
import android.view.WindowManager;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;

import java.util.ArrayList;

public class QuickFindDialog extends DialogFragment {
    public MainActivity mainActivity;
    public String findText;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        AlertDialog dialog = new AlertDialog.Builder(mainActivity)
                .setMessage("Find")
                .setPositiveButton("Ok", null)
                .setNegativeButton("Cancel", null)
                .setView(R.layout.quick_find_dialog)
                .create();
        return dialog;
    }

    @Override
    public void onCancel(DialogInterface dialog) {
        super.onCancel(dialog);
        dismiss();
    }

    @Override
    public void onResume() {
        super.onResume();
        AlertDialog dialog = (AlertDialog) getDialog();
        final EditText findText = dialog.findViewById(R.id.find_text);
        findText.setText(this.findText);
        findText.selectAll();

        findText.postDelayed(new Runnable() {
            @Override
            public void run() {
                InputMethodManager keyboard = (InputMethodManager)mainActivity.getSystemService(Context.INPUT_METHOD_SERVICE);
                keyboard.showSoftInput(findText, 0);
            }
        },50);

        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                FindData findData = new FindData("Title");
                findData.findType = FindData.Contains;
                findData.value1 = findText.getText().toString().toLowerCase();
                ArrayList<FindData> findDataList = new ArrayList<>();
                findDataList.add(findData);
                mainActivity.setFindDataList(findDataList);
                dismiss();
            }
        });
    }
}
