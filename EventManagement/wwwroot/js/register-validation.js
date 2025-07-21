// Real-time validation with immediate feedback
document.addEventListener('DOMContentLoaded', function() {
    const fullNameInput = document.querySelector('input[name="Input.FullName"]');
    const emailInput = document.querySelector('input[name="Input.Email"]');
    const passwordInput = document.querySelector('input[name="Input.Password"]');
    const confirmPasswordInput = document.querySelector('input[name="Input.ConfirmPassword"]');
    const phoneInput = document.querySelector('input[name="Input.PhoneNumber"]');
    const agreeCheckbox = document.querySelector('input[name="Input.AgreeToTerms"]');
    const submitButton = document.getElementById('submitButton');
    const submitHelp = document.getElementById('submitHelp');

    // Validation state
    const validationState = {
        fullName: false,
        email: false,
        password: false,
        confirmPassword: false,
        phone: true, // Optional field
        agree: false
    };

    // Real-time validation for Full Name
    if (fullNameInput) {
        fullNameInput.addEventListener('input', function() {
            // Clean up multiple spaces
            this.value = this.value.replace(/\s+/g, ' ');
            
            const value = this.value.trim();
            const nameRegex = /^[a-zA-ZÀ-ỹ\s]+$/;
            const isValid = value.length >= 2 && nameRegex.test(value) && !value.includes('  ');
            
            updateFieldValidation(this, isValid, 'fullName');
            if (!isValid && value.length > 0) {
                showFieldError(this, 'Full name must be at least 2 characters and contain only letters');
            } else {
                hideFieldError(this);
            }
        });
    }

    // Real-time validation for Email
    if (emailInput) {
        emailInput.addEventListener('input', function() {
            const value = this.value.trim();
            const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
            const isValid = emailRegex.test(value);
            
            updateFieldValidation(this, isValid, 'email');
            if (!isValid && value.length > 0) {
                showFieldError(this, 'Please enter a valid email address');
            } else {
                hideFieldError(this);
            }
        });
    }

    // Real-time validation for Password
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            const value = this.value;
            const hasUpper = /[A-Z]/.test(value);
            const hasLower = /[a-z]/.test(value);
            const hasNumber = /\d/.test(value);
            const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);
            const hasLength = value.length >= 8;
            const noSpaces = !/\s/.test(value);
            
            const isValid = hasUpper && hasLower && hasNumber && hasSpecial && hasLength && noSpaces;
            
            updateFieldValidation(this, isValid, 'password');
            if (!isValid && value.length > 0) {
                let errors = [];
                if (!hasLength) errors.push('8+ characters');
                if (!hasUpper) errors.push('uppercase letter');
                if (!hasLower) errors.push('lowercase letter');
                if (!hasNumber) errors.push('number');
                if (!hasSpecial) errors.push('special character');
                if (!noSpaces) errors.push('no spaces allowed');
                
                showFieldError(this, 'Password must contain: ' + errors.join(', '));
            } else {
                hideFieldError(this);
            }
            
            // Also validate confirm password if it has value
            if (confirmPasswordInput && confirmPasswordInput.value) {
                validateConfirmPassword();
            }
        });
    }

    // Real-time validation for Confirm Password
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', validateConfirmPassword);
    }

    function validateConfirmPassword() {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;
        const isValid = password === confirmPassword && confirmPassword.length > 0;
        
        updateFieldValidation(confirmPasswordInput, isValid, 'confirmPassword');
        if (!isValid && confirmPassword.length > 0) {
            showFieldError(confirmPasswordInput, 'Passwords do not match');
        } else {
            hideFieldError(confirmPasswordInput);
        }
    }

    // Real-time validation for Phone (optional)
    if (phoneInput) {
        phoneInput.addEventListener('input', function() {
            const value = this.value.trim();
            if (value === '') {
                validationState.phone = true; // Optional field
                this.classList.remove('is-valid', 'is-invalid');
                hideFieldError(this);
            } else {
                const phoneRegex = /^(\+84|0)[3|5|7|8|9][0-9]{8}$/;
                const isValid = phoneRegex.test(value.replace(/[\s-]/g, ''));
                
                updateFieldValidation(this, isValid, 'phone');
                if (!isValid) {
                    showFieldError(this, 'Please enter a valid Vietnamese phone number');
                } else {
                    hideFieldError(this);
                }
            }
            updateSubmitButton();
        });
    }

    // Real-time validation for Agreement
    if (agreeCheckbox) {
        agreeCheckbox.addEventListener('change', function() {
            const isValid = this.checked;
            updateFieldValidation(this, isValid, 'agree');
            
            if (!isValid) {
                showFieldError(this, 'You must agree to the terms and conditions');
            } else {
                hideFieldError(this);
            }
        });
    }

    // Helper functions
    function updateFieldValidation(field, isValid, fieldName) {
        validationState[fieldName] = isValid;
        
        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
        }
        
        updateSubmitButton();
    }

    function showFieldError(field, message) {
        let errorDiv = field.parentNode.querySelector('.field-validation-error');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'field-validation-error text-danger';
            field.parentNode.appendChild(errorDiv);
        }
        errorDiv.textContent = message;
    }

    function hideFieldError(field) {
        const errorDiv = field.parentNode.querySelector('.field-validation-error');
        if (errorDiv) {
            errorDiv.remove();
        }
    }

    function updateSubmitButton() {
        const allValid = Object.values(validationState).every(state => state === true);
        
        if (allValid) {
            submitButton.disabled = false;
            submitButton.classList.remove('btn-secondary');
            submitButton.classList.add('btn-custom-gold');
            submitHelp.textContent = 'Ready to create your account!';
            submitHelp.className = 'text-success text-center mt-2';
        } else {
            submitButton.disabled = true;
            submitButton.classList.remove('btn-custom-gold');
            submitButton.classList.add('btn-secondary');
            submitHelp.textContent = 'Please fill all required fields correctly to continue';
            submitHelp.className = 'text-muted text-center mt-2';
        }
    }

    // Initial validation check
    updateSubmitButton();
}); 