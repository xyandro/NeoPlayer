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
    public NetworkService networkService;
    public String address;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        AlertDialog dialog = new AlertDialog.Builder(mainActivity)
                .setMessage("Find NeoPlayer")
                .setPositiveButton("Ok", null)
                .setNegativeButton("Cancel", null)
                .setView(R.layout.get_address_dialog)
                .create();
        return dialog;
    }

    @Override
    public void onCancel(DialogInterface dialog) {
        super.onCancel(dialog);
        if (mainActivity == null)
            return;
        dismiss();
        mainActivity.finish();
    }

    @Override
    public void onResume() {
        super.onResume();
        AlertDialog dialog = (AlertDialog) getDialog();
        final EditText addressText = dialog.findViewById(R.id.address);
        addressText.setText(address);

        dialog.findViewById(R.id.detect_wifi).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String newAddress = networkService.findNeoPlayerNetwork();
                if (newAddress != null)
                    addressText.setText(newAddress);
            }
        });
        dialog.findViewById(R.id.detect_bluetooth).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String newAddress = networkService.findNeoPlayerBluetooth();
                if (newAddress != null)
                    addressText.setText(newAddress);
            }
        });
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                networkService.setNeoPlayerAddress(addressText.getText().toString());
            }
        });
        dialog.getButton(AlertDialog.BUTTON_NEGATIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                dismiss();
                mainActivity.finish();
            }
        });
    }
}
