using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	/// <summary>
	/// USE WITH EXTREME CAUTION
	/// Interface that can be used to access internal members of the EntityWrapper class.
	/// 
	/// Usage of this interface ***breaks the encapsulation principle of OOP***, 
	/// should be used only when there is no other option
	/// </summary>
	public interface IEntityWrapperInternal
    {
        /// <summary>
        /// Returns the target entity on the entity wrapper.
        /// </summary>
        /// <returns>The target entity.</returns>
        Entity GetTarget();



        /// <summary>
        /// Returns the pre-image used by the entity wrapper.
        /// </summary>
        /// <returns>The pre-image entity.</returns>
        Entity GetPreImage();



        /// <summary>
        /// Returns the merge between pre-image and target.
        /// </summary>
        /// <returns>The post-image entity.</returns>
        Entity GetPostImage();


        /// <summary>
        /// Allows users to override the Id of the current record.
        /// </summary>
        /// <param name="newId">The new GUID for the current record.</param>
        void SetId(Guid newId);
    }
}
