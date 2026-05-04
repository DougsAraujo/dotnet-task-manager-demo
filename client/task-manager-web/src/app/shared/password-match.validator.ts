import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Ensures this control matches the sibling field (e.g. password confirmation). */
export function matchField(fieldName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const parent = control.parent;
    if (!parent) {
      return null;
    }
    const other = parent.get(fieldName)?.value;
    const v = control.value as string | undefined;
    if (v === undefined || v === null || `${v}`.length === 0) {
      return null;
    }
    return v === other ? null : { mismatch: true };
  };
}
