package neoplayer.neoremote;

import android.app.AlertDialog;
import android.app.Dialog;
import android.app.DialogFragment;
import android.content.DialogInterface;
import android.os.Bundle;
import android.view.View;
import android.widget.EditText;

public class GetAddressDialog extends DialogFragment {
    public MainActivity mainActivity;
    public String address;
    EditText addressText;

    public GetAddressDialog() {
    }

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        AlertDialog dialog = new AlertDialog.Builder(getActivity())
                .setMessage("Find NeoPlayer")
                .setPositiveButton("Ok", null)
                .setNegativeButton("Detect", null)
                .setView(R.layout.get_address_dialog)
                .create();
        return dialog;
    }

    @Override
    public void onResume() {
        super.onResume();
        AlertDialog dialog = (AlertDialog) getDialog();
        dialog.setCancelable(false);
        addressText = dialog.findViewById(R.id.address);
        addressText.setText(address);
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setAddress(addressText.getText().toString());
            }
        });
        dialog.getButton(AlertDialog.BUTTON_NEGATIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                String newAddress = mainActivity.findNeoPlayer();
                if (newAddress != null)
                    addressText.setText(newAddress);
            }
        });
    }
}
